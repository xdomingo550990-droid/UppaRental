using RentalManagementSystem.Model;

namespace RentalManagementSystem.Services
{
    public static class UserSession
    {
        public static User? CurrentUser { get; set; }

        public static int UserId => CurrentUser?.getUserId() ?? CurrentUser?.UserId ?? 0;

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}