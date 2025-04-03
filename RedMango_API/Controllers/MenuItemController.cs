using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedMango_API.Data;
using RedMango_API.Model;
using RedMango_API.Model.Dto;
using RedMango_API.Services;
using RedMango_API.Utility;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace RedMango_API.Controllers
{
    [Route("api/MenuItem")]
    [ApiController]
    public class MenuItemController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IBlobService _blobService;
        private ApiResponse _response;
        public MenuItemController(ApplicationDbContext db, IBlobService blobService)
        {
            _db = db;
            _response = new ApiResponse();
            _blobService = blobService;
        }
        [HttpGet]
        public async Task<IActionResult> GetMenuItems()
        {
            _response.Result = _db.MenuItems;
            _response.statusCode = HttpStatusCode.OK;
            return Ok( _response);
        }

        [HttpGet("id:int", Name = "GetMenuItem")]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            if(id == 0)
            {
                _response.statusCode = HttpStatusCode.BadRequest;
                _response.isSuccess = false;
                return BadRequest(_response);
            }
            MenuItem menuItem = _db.MenuItems.FirstOrDefault(x => x.Id == id);
            if(menuItem == null)
            {
                _response.statusCode = HttpStatusCode.NotFound;
                _response.isSuccess = false;
                return NotFound(_response);
            }
            _response.Result = menuItem;
            _response.statusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMenuItem([FromForm]MenuItemCreateDto menuItemCreateDto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if(menuItemCreateDto == null || menuItemCreateDto.File.Length == 0)
                    {
                        _response.statusCode = HttpStatusCode.BadRequest;
                        _response.isSuccess = false;
                        return BadRequest();
                    }
                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(menuItemCreateDto.File.FileName)}";
                    MenuItem menuItemCreate = new()
                    {
                        Name = menuItemCreateDto.Name,
                        Description = menuItemCreateDto.Description,
                        SpecialTag = menuItemCreateDto.SpecialTag,
                        Category = menuItemCreateDto.Category,
                        Price = menuItemCreateDto.Price,
                        Image = await _blobService.UploadBlob(fileName, SD.SD_Storage_Container, menuItemCreateDto.File)
                    };
                    _db.MenuItems.Add(menuItemCreate);
                    _db.SaveChanges();
                    _response.Result = menuItemCreate;
                    _response.statusCode = HttpStatusCode.Created;
                    return CreatedAtRoute("GetMenuItem", new { id = menuItemCreate.Id }, _response);

                }   
                else
                {
                    _response.isSuccess = false;
                }
            }
            catch(Exception ex)
            {
                _response.isSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString()};
            }
            _response.statusCode = HttpStatusCode.BadRequest;
            _response.isSuccess = false;
            return BadRequest(_response);

        }

        [HttpPut("id:int")]
        public async Task<ActionResult> UpdateMenuItem(int id, [FromForm] MenuItemUpdateDto menuItemUpdateDto)
        {
            try 
            {
                if (ModelState.IsValid)
                {
                    if(menuItemUpdateDto == null || id!= menuItemUpdateDto.Id)
                    {
                        _response.statusCode = HttpStatusCode.BadRequest;
                        _response.isSuccess = false;
                        return BadRequest();
                    }

                    MenuItem menuItemFromDb = await _db.MenuItems.FindAsync(id);
                    if(menuItemFromDb == null)
                    {
                        _response.statusCode = HttpStatusCode.BadRequest;
                        _response.isSuccess = false;
                        return BadRequest();
                    }
                    menuItemFromDb.Name = menuItemUpdateDto.Name;
                    menuItemFromDb.Description = menuItemUpdateDto.Description;
                    menuItemFromDb.SpecialTag = menuItemUpdateDto.SpecialTag;
                    menuItemFromDb.Category = menuItemUpdateDto.Category;
                    menuItemFromDb.Price = menuItemUpdateDto.Price;
                    if(menuItemUpdateDto.File !=null && menuItemUpdateDto.File.Length > 0)
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(menuItemUpdateDto.File.FileName)}";
                        await _blobService.DeleteBlob(menuItemFromDb.Image.Split("/").Last(), SD.SD_Storage_Container);
                        menuItemFromDb.Image = await _blobService.UploadBlob(fileName, SD.SD_Storage_Container, menuItemUpdateDto.File);
                    }
                    _db.MenuItems.Update(menuItemFromDb);
                    _db.SaveChanges();
                    _response.statusCode = HttpStatusCode.NoContent;
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
                _response.isSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            _response.statusCode = HttpStatusCode.BadRequest;
            _response.isSuccess = false;
            return BadRequest(_response);
        }

        [HttpDelete("id:int")]
        public async Task<ActionResult> DeleteMenuItem(int id)
        {
            try
            {
                if(id == 0)
                {
                    _response.statusCode = HttpStatusCode.BadRequest;
                    _response.isSuccess = false;
                    return BadRequest();
                }
                MenuItem menuItemFromDb = await _db.MenuItems.FindAsync(id);
                if (menuItemFromDb == null)
                {
                    _response.statusCode = HttpStatusCode.BadRequest;
                    _response.isSuccess = false;
                    return BadRequest();
                }
                await _blobService.DeleteBlob(menuItemFromDb.Image.Split("/").Last(), SD.SD_Storage_Container);
                int millisecond = 2000;
                Thread.Sleep(millisecond);

                _db.MenuItems.Remove(menuItemFromDb);
                _db.SaveChanges();
                _response.statusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch(Exception ex)
            {
                _response.isSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            _response.statusCode = HttpStatusCode.BadRequest;
            _response.isSuccess = false;
            return BadRequest(_response);
        }
    }
}
