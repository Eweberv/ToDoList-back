using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoList.Models;
using TodoList_back.Data;

namespace TodoList_back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TodoListController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TodoListController> _logger;

        public TodoListController(ApplicationDbContext context, ILogger<TodoListController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<TodoItem>> Get()
        {
            var todos = _context.TodoItems.ToList();
            return Ok(todos);
        }

        [HttpGet("{id}")]
        public ActionResult<TodoItem> Get(int id)
        {
            var todoItem = _context.TodoItems.Find(id);
            if (todoItem == null)
            {
                return NotFound();
            }
            return Ok(todoItem);
        }
        
        [HttpGet("myToDos")]
        public ActionResult<IEnumerable<TodoItem>> GetMyTodos()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var todos = _context.TodoItems.Where(t => t.UserId == userId).ToList();

            if (todos == null || !todos.Any())
            {
                return NotFound("No todo items found for the current user.");
            }
            return Ok(todos);
        }

        [HttpPost]
        public ActionResult<TodoItem> Post([FromBody] TodoItem newTodo)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            newTodo.UserId = userId;

            newTodo.IsCompleted = false;

            _context.TodoItems.Add(newTodo);
            _context.SaveChanges();

            return CreatedAtAction(nameof(Get), new { id = newTodo.Id }, newTodo);
        }

        [HttpPut("{id}")]
        public ActionResult Put(int id, [FromBody] TodoItem updatedTodo)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var todoItem = _context.TodoItems.FirstOrDefault(t => t.Id == id && t.UserId == userId);
            
            if (todoItem == null)
            {
                return NotFound();
            }

            todoItem.Title = updatedTodo.Title;
            todoItem.IsCompleted = updatedTodo.IsCompleted;
            _context.SaveChanges();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var todoItem = _context.TodoItems.FirstOrDefault(t => t.Id == id && t.UserId == userId);
            
            if (todoItem == null)
            {
                return NotFound();
            }
            _context.TodoItems.Remove(todoItem);
            _context.SaveChanges();
            return NoContent();
        }
    }
}
