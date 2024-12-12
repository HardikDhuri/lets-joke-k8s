using HardikDhuri.TaskManager.Api.Data;
using HardikDhuri.TaskManager.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardikDhuri.TaskManager.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TaskController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly RabbitMqService _rabbitMqService;

    public TaskController(ApplicationDbContext context, RabbitMqService rabbitMqService)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
    }

    [HttpPost]
    public IActionResult CreateTask()
    {
        var task = new Models.Task
        {
            Description = "Fetch a random joke"
        };

        _context.Tasks.Add(task);
        _context.SaveChanges();

        // Send the task ID to RabbitMQ
        _rabbitMqService.SendMessage(task.Id.ToString());

        return Ok(new { Message = "Task created", TaskId = task.Id });
    }

    [HttpGet]
    public IActionResult GetTasks()
    {
        var tasks = _context.Tasks.ToList();
        return Ok(tasks);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        var task = _context.Tasks.Find(id);
        if (task == null) return NotFound();

        task.Result = request.Result;
        task.Status = "Completed";
        _context.SaveChanges();

        return Ok(new { Message = "Task updated successfully" });
    }
}

public class UpdateTaskRequest
{
    public string Result { get; set; } = string.Empty;
}