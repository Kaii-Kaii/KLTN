namespace BE_QLTiemThuoc.Services
{
    public class ApiResponse<T>
    {
        public int Status { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
    public static class ApiResponseHelper
    {
        public static ApiResponse<T> ExecuteSafety<T>(Func<T> func)
        {
            try
            {
                var result = func();
                return new ApiResponse<T>
                {
                    Status = 1,
                    Message = null,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                // Return full exception details in Message to help debugging (consider removing in production)
                var full = ex.Message;
                try { if (ex.InnerException != null) full += " | Inner: " + ex.InnerException.Message; } catch { }
                try { full += "\n" + ex.ToString(); } catch { }
                return new ApiResponse<T>
                {
                    Status = -1,
                    Message = full,
                    Data = default
                };
            }
        }

        public static async Task<ApiResponse<T>> ExecuteSafetyAsync<T>(Func<Task<T>> func)
        {
            try
            {
                var result = await func();
                return new ApiResponse<T>
                {
                    Status = 1,
                    Message = null,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                // Return full exception details in Message to help debugging (consider removing in production)
                var full = ex.Message;
                try { if (ex.InnerException != null) full += " | Inner: " + ex.InnerException.Message; } catch { }
                try { full += "\n" + ex.ToString(); } catch { }
                return new ApiResponse<T>
                {
                    Status = -1,
                    Message = full,
                    Data = default
                };
            }
        }
    }

}
