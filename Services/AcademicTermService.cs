using Financial_management_backend.Data;

namespace Financial_management_backend.Services
{
    public interface IAcademicTermService
    {
        (string term, int year) GetCurrentAcademicTerm();
        (string term, int year) GetAcademicTermForDate(DateTime date);
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
    }
}