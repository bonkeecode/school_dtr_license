using System;
using System.Collections.Generic;

namespace SchoolDTR.Models;

public class EmployeeDtrPrintData
{
    public string EmployeeNo { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string PositionTitle { get; set; } = "";
    public string SchoolId { get; set; } = "";

    public string ImmediateSupervisorName { get; set; } = "";
    public string ImmediateSupervisorPosition { get; set; } = "";

    public DateTime Month { get; set; }
    public List<EmployeeDtrPrintRow> Rows { get; set; } = new();
}

public class EmployeeDtrPrintRow
{
    public DateTime Date { get; set; }

    public string MorningIn { get; set; } = "";
    public string MorningOut { get; set; } = "";
    public string AfternoonIn { get; set; } = "";
    public string AfternoonOut { get; set; } = "";

    public string Remarks { get; set; } = "";

    // FULL DAY merge (Weekend, Travel, Event, Holiday etc.)
    public string MergedStatus
    {
        get
        {
            var mi = MorningIn.Trim();
            var mo = MorningOut.Trim();
            var ai = AfternoonIn.Trim();
            var ao = AfternoonOut.Trim();

            if (string.IsNullOrWhiteSpace(mi) ||
                string.IsNullOrWhiteSpace(mo) ||
                string.IsNullOrWhiteSpace(ai) ||
                string.IsNullOrWhiteSpace(ao))
                return "";

            return mi == mo &&
                   mo == ai &&
                   ai == ao
                ? mi
                : "";
        }
    }

    // MORNING HALF-DAY merge
    public string MergedMorningStatus
    {
        get
        {
            var mi = MorningIn.Trim();
            var mo = MorningOut.Trim();

            if (string.IsNullOrWhiteSpace(mi) ||
                string.IsNullOrWhiteSpace(mo))
                return "";

            return mi == mo ? mi : "";
        }
    }

    // AFTERNOON HALF-DAY merge
    public string MergedAfternoonStatus
    {
        get
        {
            var ai = AfternoonIn.Trim();
            var ao = AfternoonOut.Trim();

            if (string.IsNullOrWhiteSpace(ai) ||
                string.IsNullOrWhiteSpace(ao))
                return "";

            return ai == ao ? ai : "";
        }
    }
}
