using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/meetingrooms")]
public class DashboardMeetingRoomController : ControllerBase
{
    private readonly IMeetingRoomService _meetingRoomService;
    private readonly ILogger<DashboardMeetingRoomController> _logger;

    public DashboardMeetingRoomController(
        IMeetingRoomService meetingRoomService,
        ILogger<DashboardMeetingRoomController> logger)
    {
        _meetingRoomService = meetingRoomService;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有會議室列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MeetingRoomResponse>>> GetMeetingRooms([FromQuery] string? counterId)
    {
        try
        {
            IEnumerable<MeetingRoomResponse> meetingRooms;
            if (!string.IsNullOrEmpty(counterId))
            {
                meetingRooms = await _meetingRoomService.GetMeetingRoomsByCounterIdWithCounterNameAsync(counterId);
            }
            else
            {
                meetingRooms = await _meetingRoomService.GetAllMeetingRoomsWithCounterNameAsync();
            }
            return Ok(meetingRooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得會議室列表失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 根據 ID 取得會議室資訊
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MeetingRoomResponse>> GetMeetingRoom(string id)
    {
        try
        {
            var meetingRoom = await _meetingRoomService.GetMeetingRoomByIdWithCounterNameAsync(id);
            if (meetingRoom == null)
            {
                return NotFound(new { error = "會議室不存在" });
            }
            return Ok(meetingRoom);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得會議室資訊失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 建立新會議室
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MeetingRoom>> CreateMeetingRoom([FromBody] MeetingRoom meetingRoom)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(meetingRoom.Id))
            {
                return BadRequest(new { error = "會議室 ID 為必填欄位" });
            }

            if (string.IsNullOrWhiteSpace(meetingRoom.Name))
            {
                return BadRequest(new { error = "會議室名稱為必填欄位" });
            }

            if (string.IsNullOrWhiteSpace(meetingRoom.CounterId))
            {
                return BadRequest(new { error = "所屬櫃檯 ID 為必填欄位" });
            }

            // 檢查 ID 是否已存在
            var existingMeetingRoom = await _meetingRoomService.GetMeetingRoomByIdAsync(meetingRoom.Id);
            if (existingMeetingRoom != null)
            {
                return Conflict(new { error = "會議室 ID 已存在" });
            }

            var createdMeetingRoom = await _meetingRoomService.CreateMeetingRoomAsync(meetingRoom);
            return CreatedAtAction(nameof(GetMeetingRoom), new { id = createdMeetingRoom.Id }, createdMeetingRoom);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立會議室失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 更新會議室資訊
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<MeetingRoom>> UpdateMeetingRoom(string id, [FromBody] MeetingRoom meetingRoom)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(meetingRoom.Name))
            {
                return BadRequest(new { error = "會議室名稱為必填欄位" });
            }

            if (string.IsNullOrWhiteSpace(meetingRoom.CounterId))
            {
                return BadRequest(new { error = "所屬櫃檯 ID 為必填欄位" });
            }

            var updatedMeetingRoom = await _meetingRoomService.UpdateMeetingRoomAsync(id, meetingRoom);
            if (updatedMeetingRoom == null)
            {
                return NotFound(new { error = "會議室不存在" });
            }

            return Ok(updatedMeetingRoom);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新會議室失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 刪除會議室
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMeetingRoom(string id)
    {
        try
        {
            var deleted = await _meetingRoomService.DeleteMeetingRoomAsync(id);
            if (!deleted)
            {
                return NotFound(new { error = "會議室不存在" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除會議室失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }
}

