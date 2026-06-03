using System;
using System.Drawing;
using SchoolDTR.Models;

namespace SchoolDTR.Forms;

public static class CscForm48Printer
{
    public static void DrawForm(Graphics g, Rectangle bounds, EmployeeDtrPrintData data)
    {
        using var fontTiny = new Font("Times New Roman", 7);
        using var fontSmall = new Font("Times New Roman", 8);
        using var fontNormal = new Font("Times New Roman", 9);
        using var fontBold = new Font("Times New Roman", 9, FontStyle.Bold);
        using var fontTitle = new Font("Times New Roman", 13, FontStyle.Bold);

        int x = bounds.Left + 45;
        int y = bounds.Top + 35;
        int w = bounds.Width - 90;

        g.DrawString("CS Form 48", fontSmall, Brushes.Black, x, y);

        Center(g, "DAILY TIME RECORD", fontTitle, x, y + 10, w);

        g.DrawString("Official Time", fontSmall, Brushes.Black, x + w - 105, y);
        g.DrawString("A.M. - 8:00 - 12:00", fontSmall, Brushes.Black, x + w - 105, y + 12);
        g.DrawString("P.M. - 1:00 - 5:00", fontSmall, Brushes.Black, x + w - 105, y + 24);

        y += 55;

        g.DrawString("Name:", fontNormal, Brushes.Black, x, y);
        g.DrawString(data.EmployeeName.ToUpper(), fontBold, Brushes.Black, x + 38, y);
        g.DrawLine(Pens.Black, x + 38, y + 13, x + 270, y + 13);

        y += 17;

        g.DrawString("For the month of", fontNormal, Brushes.Black, x, y);
        g.DrawString(data.Month.ToString("MMMM yyyy").ToUpper(), fontBold, Brushes.Black, x + 95, y);
        g.DrawLine(Pens.Black, x + 95, y + 13, x + 185, y + 13);

        y += 22;

        int tableX = x;

        int colDay = 65;
        int colMorningArr = 130;
        int colMorningDep = 130;
        int colAfternoonArr = 130;
        int colAfternoonDep = 130;
        int colOver = 145;

        int header1H = 18;
        int header2H = 18;
        int rowH = 18;

        int morningX = tableX + colDay;
        int morningDepX = morningX + colMorningArr;
        int afternoonX = morningDepX + colMorningDep;
        int afternoonDepX = afternoonX + colAfternoonArr;
        int overX = afternoonDepX + colAfternoonDep;
        int logColumnsW = colMorningArr + colMorningDep + colAfternoonArr + colAfternoonDep;

        DrawCell(g, "Day", fontSmall, tableX, y, colDay, header1H + header2H);
        DrawCell(g, "Morning", fontSmall, morningX, y, colMorningArr + colMorningDep, header1H);
        DrawCell(g, "Afternoon", fontSmall, afternoonX, y, colAfternoonArr + colAfternoonDep, header1H);
        DrawCell(g, "Over/\nUnder time", fontSmall, overX, y, colOver, header1H + header2H);

        y += header1H;

        DrawCell(g, "Arrival", fontTiny, morningX, y, colMorningArr, header2H);
        DrawCell(g, "Departure", fontTiny, morningDepX, y, colMorningDep, header2H);
        DrawCell(g, "Arrival", fontTiny, afternoonX, y, colAfternoonArr, header2H);
        DrawCell(g, "Departure", fontTiny, afternoonDepX, y, colAfternoonDep, header2H);

        y += header2H;

        int daysInMonth = DateTime.DaysInMonth(data.Month.Year, data.Month.Month);

        for (int day = 1; day <= 31; day++)
        {
            var row = data.Rows.Find(r => r.Date.Day == day);

            DrawCell(g, day <= daysInMonth ? day.ToString() : "", fontBold, tableX, y, colDay, rowH);

            if (row != null && row.MergedStatus != "")
            {
                string currentStatus = row.MergedStatus;
                int spanDays = 1;

                while (day + spanDays <= daysInMonth)
                {
                    var nextRow = data.Rows.Find(r => r.Date.Day == day + spanDays);

                    if (nextRow == null || nextRow.MergedStatus != currentStatus)
                        break;

                    spanDays++;
                }

                int mergedHeight = rowH * spanDays;

                DrawCell(g, currentStatus, fontBold, morningX, y, logColumnsW, mergedHeight);

                for (int i = 0; i < spanDays; i++)
                {
                    DrawCell(g, "", fontSmall, overX, y + (rowH * i), colOver, rowH);

                    if (i > 0)
                    {
                        DrawCell(g, (day + i).ToString(), fontBold, tableX, y + (rowH * i), colDay, rowH);
                    }
                }

                y += mergedHeight;
                day += spanDays - 1;
                continue;
            }

            if (row != null)
            {
                DrawTimeCells(
                    g,
                    row,
                    fontSmall,
                    fontBold,
                    morningX,
                    morningDepX,
                    afternoonX,
                    afternoonDepX,
                    colMorningArr,
                    colMorningDep,
                    colAfternoonArr,
                    colAfternoonDep,
                    y,
                    rowH);
            }
            else
            {
                DrawCell(g, "", fontSmall, morningX, y, colMorningArr, rowH);
                DrawCell(g, "", fontSmall, morningDepX, y, colMorningDep, rowH);
                DrawCell(g, "", fontSmall, afternoonX, y, colAfternoonArr, rowH);
                DrawCell(g, "", fontSmall, afternoonDepX, y, colAfternoonDep, rowH);
            }

            DrawCell(g, "", fontSmall, overX, y, colOver, rowH);

            y += rowH;
        }

        DrawCell(g, "Total Number of Days Present", fontBold, tableX, y, colDay + logColumnsW, rowH);
        DrawCell(g, "", fontSmall, overX, y, colOver, rowH);
        y += rowH;

        DrawCell(g, "Total Number of Days Absent", fontBold, tableX, y, colDay + logColumnsW, rowH);
        DrawCell(g, "", fontSmall, overX, y, colOver, rowH);
        y += rowH;

        DrawCell(g, "Total Over/Under Time", fontBold, tableX, y, colDay + logColumnsW, rowH);
        DrawCell(g, "", fontSmall, overX, y, colOver, rowH);

        y += 22;

        string cert =
            "I certify on my honor that the above is a true and correct report of the " +
            "hours of work performed, record of which was made daily at the time " +
            "of arrival and departure from office.";

        CenterWrapped(g, cert, fontSmall, tableX + 220, y, 300, 45);

        y += 58;

        Center(g, data.EmployeeName.ToUpper(), fontBold, tableX + 220, y, 300);
        g.DrawLine(Pens.Black, tableX + 255, y + 14, tableX + 485, y + 14);
        y += 15;
        Center(g, "Employee", fontSmall, tableX + 220, y, 300);

        y += 35;

        g.DrawString("Verified as to the prescribed office hours:", fontSmall, Brushes.Black, tableX, y);

        y += 45;

        string supervisorName = (data.ImmediateSupervisorName ?? "").Trim();
        string supervisorPosition = (data.ImmediateSupervisorPosition ?? "").Trim();

        if (!string.IsNullOrWhiteSpace(supervisorName))
        {
            Center(g, supervisorName.ToUpperInvariant(), fontBold, tableX + 220, y, 300);
            g.DrawLine(Pens.Black, tableX + 255, y + 14, tableX + 485, y + 14);
            y += 15;

            if (!string.IsNullOrWhiteSpace(supervisorPosition))
                Center(g, supervisorPosition, fontSmall, tableX + 220, y, 300);
            else
                Center(g, "Immediate Supervisor", fontSmall, tableX + 220, y, 300);
        }
        else
        {
            Center(g, "______________________________", fontBold, tableX + 220, y, 300);
            y += 15;
            Center(g, "Immediate Supervisor", fontSmall, tableX + 220, y, 300);
        }
    }

    private static void DrawTimeCells(
        Graphics g,
        EmployeeDtrPrintRow row,
        Font fontSmall,
        Font fontBold,
        int morningX,
        int morningDepX,
        int afternoonX,
        int afternoonDepX,
        int colMorningArr,
        int colMorningDep,
        int colAfternoonArr,
        int colAfternoonDep,
        int y,
        int rowH)
    {
        string morningStatus = row.MergedMorningStatus;
        string afternoonStatus = row.MergedAfternoonStatus;

        if (!string.IsNullOrWhiteSpace(morningStatus))
        {
            DrawCell(g, morningStatus, fontBold, morningX, y, colMorningArr + colMorningDep, rowH);
        }
        else
        {
            DrawCell(g, row.MorningIn ?? "", fontSmall, morningX, y, colMorningArr, rowH);
            DrawCell(g, row.MorningOut ?? "", fontSmall, morningDepX, y, colMorningDep, rowH);
        }

        if (!string.IsNullOrWhiteSpace(afternoonStatus))
        {
            DrawCell(g, afternoonStatus, fontBold, afternoonX, y, colAfternoonArr + colAfternoonDep, rowH);
        }
        else
        {
            DrawCell(g, row.AfternoonIn ?? "", fontSmall, afternoonX, y, colAfternoonArr, rowH);
            DrawCell(g, row.AfternoonOut ?? "", fontSmall, afternoonDepX, y, colAfternoonDep, rowH);
        }
    }

    private static void DrawCell(Graphics g, string text, Font font, int x, int y, int w, int h)
    {
        g.DrawRectangle(Pens.Black, x, y, w, h);

        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.DrawString(text, font, Brushes.Black, new RectangleF(x + 2, y + 1, w - 4, h - 2), sf);
    }

    private static void Center(Graphics g, string text, Font font, int x, int y, int w)
    {
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.DrawString(text, font, Brushes.Black, new RectangleF(x, y, w, font.Height + 4), sf);
    }

    private static void CenterWrapped(Graphics g, string text, Font font, int x, int y, int w, int h)
    {
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near
        };

        g.DrawString(text, font, Brushes.Black, new RectangleF(x, y, w, h), sf);
    }
}
