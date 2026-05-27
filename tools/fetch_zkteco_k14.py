#!/usr/bin/env python3
"""
ZKTeco K14 fetch bridge for School DTR.

Requirements on the school AO computer:
    pip install pyzk pymysql

This script connects to the ZKTeco K14 over LAN, pulls users + attendance logs,
and inserts them into MySQL biometric_raw_logs using a UNIQUE KEY to avoid duplicates.
"""

import argparse
import datetime as dt
import json
import sys

try:
    import pymysql
except ImportError:
    print(json.dumps({
        "success": False,
        "message": "Missing Python package: pymysql. Run: pip install pymysql pyzk"
    }))
    sys.exit(2)

try:
    from zk import ZK
except ImportError:
    print(json.dumps({
        "success": False,
        "message": "Missing Python package: pyzk. Run: pip install pymysql pyzk"
    }))
    sys.exit(2)


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--school", required=True)
    parser.add_argument("--ip", required=True)
    parser.add_argument("--port", type=int, default=4370)
    parser.add_argument("--machine", type=int, default=1)
    parser.add_argument("--from", dest="date_from", required=True)
    parser.add_argument("--to", dest="date_to", required=True)
    return parser.parse_args()


def get_mysql_connection():
    return pymysql.connect(
        host="localhost",
        user="root",
        password="#P4ssword1",
        database="school_dtr_305680",
        charset="utf8mb4",
        cursorclass=pymysql.cursors.DictCursor
    )


def safe_text(value):
    if value is None:
        return None

    value = str(value).strip()

    if value == "":
        return None

    return value


def build_user_name_map(device_conn):
    """
    Returns:
        {
            "4": "Juan Dela Cruz",
            "7": "Maria Santos"
        }
    """
    users = {}

    try:
        device_users = device_conn.get_users()
    except Exception:
        return users

    for user in device_users:
        user_id = safe_text(getattr(user, "user_id", None))
        name = safe_text(getattr(user, "name", None))

        if user_id and name:
            users[user_id] = name

        # Some pyzk versions expose uid separately.
        uid = safe_text(getattr(user, "uid", None))
        if uid and name and uid not in users:
            users[uid] = name

    return users


def main():
    args = parse_args()
    date_from = dt.datetime.strptime(args.date_from, "%Y-%m-%d").date()
    date_to = dt.datetime.strptime(args.date_to, "%Y-%m-%d").date()

    zk = ZK(args.ip, port=args.port, timeout=15, password=0, force_udp=False, ommit_ping=False)

    conn = None
    total = 0
    inserted = 0
    duplicates = 0
    updated_names = 0
    device_serial = None

    try:
        conn = zk.connect()
        conn.disable_device()

        try:
            device_serial = conn.get_serialnumber()
        except Exception:
            device_serial = None

        user_names = build_user_name_map(conn)
        attendance = conn.get_attendance()

        db = get_mysql_connection()

        try:
            with db.cursor() as cur:
                for log in attendance:
                    punch_time = log.timestamp
                    if punch_time is None:
                        continue

                    punch_date = punch_time.date()
                    if punch_date < date_from or punch_date > date_to:
                        continue

                    total += 1

                    biometric_user_id = safe_text(getattr(log, "user_id", None))
                    if not biometric_user_id:
                        continue

                    employee_name = user_names.get(biometric_user_id)
                    punch_type = safe_text(getattr(log, "punch", None)) or ""

                    sql = """
                        INSERT INTO biometric_raw_logs
                            (
                                school_id,
                                biometric_user_id,
                                employee_name,
                                punch_time,
                                punch_type,
                                device_serial,
                                fetched_at
                            )
                        VALUES
                            (%s, %s, %s, %s, %s, %s, NOW())
                        ON DUPLICATE KEY UPDATE
                            employee_name = COALESCE(NULLIF(VALUES(employee_name), ''), employee_name),
                            punch_type = VALUES(punch_type),
                            device_serial = VALUES(device_serial),
                            fetched_at = NOW()
                    """

                    cur.execute(sql, (
                        args.school,
                        biometric_user_id,
                        employee_name,
                        punch_time.strftime("%Y-%m-%d %H:%M:%S"),
                        punch_type,
                        device_serial,
                    ))

                    if cur.rowcount == 1:
                        inserted += 1
                    elif cur.rowcount == 2:
                        duplicates += 1
                        if employee_name:
                            updated_names += 1
                    else:
                        duplicates += 1

                if device_serial:
                    cur.execute("""
                        UPDATE biometric_devices
                        SET biometric_serial = %s
                        WHERE school_id = %s
                          AND device_ip = %s
                          AND is_active = 1
                    """, (device_serial, args.school, args.ip))

            db.commit()

        except Exception:
            db.rollback()
            raise
        finally:
            db.close()

        print(json.dumps({
            "success": True,
            "totalLogs": total,
            "insertedLogs": inserted,
            "duplicateLogs": duplicates,
            "updatedNames": updated_names,
            "message": (
                f"Fetched {total} logs from ZKTeco K14. "
                f"Inserted {inserted}, duplicates/updated {duplicates}, "
                f"names updated {updated_names}. "
                f"Device serial: {device_serial or 'N/A'}"
            )
        }))

    finally:
        if conn:
            try:
                conn.enable_device()
                conn.disconnect()
            except Exception:
                pass


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(json.dumps({
            "success": False,
            "message": str(exc),
            "totalLogs": 0,
            "insertedLogs": 0,
            "duplicateLogs": 0
        }))
        sys.exit(1)