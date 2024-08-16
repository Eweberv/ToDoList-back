using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using TodoList.Models;
using TodoList_back.Data;

namespace ToDoList_back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpPost]
        public ActionResult<TodoItem> Post([FromBody] TodoItem newTodo)
        {
            _context.TodoItems.Add(newTodo);
            _context.SaveChanges();
            return CreatedAtAction(nameof(Get), new { id = newTodo.Id }, newTodo);
        }

        [HttpPut("{id}")]
        public ActionResult Put(int id, [FromBody] TodoItem updatedTodo)
        {
            var todoItem = _context.TodoItems.Find(id);
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
            var todoItem = _context.TodoItems.Find(id);
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
