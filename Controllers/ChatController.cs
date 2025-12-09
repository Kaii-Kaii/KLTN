using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_QLTiemThuoc.Services;
using BE_QLTiemThuoc.Dto;

namespace BE_QLTiemThuoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // ?? Yêu c?u ??ng nh?p cho t?t c? API chat
    public class ChatController : ControllerBase
    {
    private readonly ChatService _service;
        public ChatController(ChatService service){ _service = service; }

        [HttpPost("conversations")]
   public async Task<IActionResult> CreateConversation([FromBody] ChatCreateConversationDto dto)
         => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.CreateConversationAsync(dto)));
        
        // For client: get or create conversation by MaKH (reuses existing if exists)
     [HttpGet("conversations/by-kh/{maKH}")]
   public async Task<IActionResult> GetOrCreateConversationByKh(string maKH, [FromQuery] int take =50)
 => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetConversationWithMessagesAsync(maKH, take)));

        [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] ChatCreateMessageDto dto)
        => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.CreateMessageAsync(dto)));

   [HttpGet("conversations/{id}/messages")]
     public async Task<IActionResult> GetMessages(long id, [FromQuery]int skip=0, [FromQuery]int take=50)
        => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetMessagesAsync(id, skip, take)));

  // Admin: list all conversations with last message and unanswered flag
     [HttpGet("conversations")]
        [Authorize(Policy = "AdminOrStaff")]  // ?? Ch? Admin ho?c Staff xem t?t c? conversations
    public async Task<IActionResult> GetConversationSummaries([FromQuery]int skip=0, [FromQuery]int take=50, [FromQuery]bool? onlyUnanswered=null)
        => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetConversationSummariesAsync(skip, take, onlyUnanswered)));
    }
}
