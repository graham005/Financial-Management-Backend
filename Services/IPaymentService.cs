using Financial_management_backend.Models;
using Financial_management_backend.Services.Dtos;

namespace Financial_management_backend.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(PaymentDto paymentDto, Guid userId);
        Task<Payment> GetPaymentByIdAsync(Guid id);
        Task<IEnumerable<Payment>> GetPaymentsByStudentAsync(Guid studentId);
        Task<AvailableFeesDto> GetAvailableFeesForStudentAsync(Guid studentId);
    }
}