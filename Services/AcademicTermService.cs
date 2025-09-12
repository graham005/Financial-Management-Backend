using Financial_management_backend.Data;

namespace Financial_management_backend.Services
{
    public interface IAcademicTermService
    {
        (string term, int year) GetCurrentAcademicTerm();
        (string term, int year) GetAcademicTermForDate(DateTime date);
        (string term, int year) GetNextAcademicTerm();
        bool IsValidTerm(string term);
        int CompareTerms(string term1, string term2);
    }

    public class AcademicTermService : IAcademicTermService
    {
        public (string term, int year) GetCurrentAcademicTerm()
        {
            return GetAcademicTermForDate(DateTime.Now);
        }

        public (string term, int year) GetAcademicTermForDate(DateTime date)
        {
            var year = date.Year;
            var month = date.Month;

            return month switch
            {
                >= 1 and <= 4 => ("Term 1", year),
                >= 5 and <= 8 => ("Term 2", year),
                >= 9 and <= 12 => ("Term 3", year),
                _ => ("Term 1", year)
            };
        }

        public (string term, int year) GetNextAcademicTerm()
        {
            var (currentTerm, currentYear) = GetCurrentAcademicTerm();
            
            if (currentTerm == "Term 1")
                return ("Term 2", currentYear);
            else if (currentTerm == "Term 2")
                return ("Term 3", currentYear);
            else // Term 3
                return ("Term 1", currentYear + 1);
        }

        public bool IsValidTerm(string term)
        {
            return term == "Term 1" || term == "Term 2" || term == "Term 3";
        }

        public int CompareTerms(string term1, string term2)
        {
            var termOrder = new Dictionary<string, int>
            {
                { "Term 1", 1 },
                { "Term 2", 2 },
                { "Term 3", 3 }
            };
            
            if (!termOrder.ContainsKey(term1) || !termOrder.ContainsKey(term2))
                throw new ArgumentException("Invalid term names");
                
            return termOrder[term1].CompareTo(termOrder[term2]);
        }
    }
}