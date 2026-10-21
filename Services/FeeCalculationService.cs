namespace ParkEase.Services
{
    public interface IFeeCalculationService
    {
        (int BillableHours, decimal EstimatedFee) CalculateEstimatedFee(decimal hourlyRate, DateTime start, DateTime end);
        (int BillableHours, decimal FinalFee) CalculateActualFee(decimal hourlyRate, DateTime checkIn, DateTime checkOut);
    }

    public class FeeCalculationService : IFeeCalculationService
    {
        public (int BillableHours, decimal EstimatedFee) CalculateEstimatedFee(decimal hourlyRate, DateTime start, DateTime end)
        {
            if (end <= start || hourlyRate < 0)
            {
                return (0, 0m);
            }

            var duration = end - start;
            var totalHours = duration.TotalHours;

            // Round partial hour up to the next billable hour, minimum 1 hour
            int billableHours = (int)Math.Ceiling(totalHours);
            if (billableHours < 1)
            {
                billableHours = 1;
            }

            decimal estimatedFee = billableHours * hourlyRate;
            return (billableHours, estimatedFee);
        }

        public (int BillableHours, decimal FinalFee) CalculateActualFee(decimal hourlyRate, DateTime checkIn, DateTime checkOut)
        {
            if (checkOut <= checkIn || hourlyRate < 0)
            {
                return (1, hourlyRate);
            }

            var duration = checkOut - checkIn;
            var totalHours = duration.TotalHours;

            int billableHours = (int)Math.Ceiling(totalHours);
            if (billableHours < 1)
            {
                billableHours = 1;
            }

            decimal finalFee = billableHours * hourlyRate;
            return (billableHours, finalFee);
        }
    }
}
