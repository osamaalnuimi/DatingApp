
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/Users")]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _photoService = photoService;
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<PagedList<MemberDto>>> GetUsers([FromQuery] UserParams userParams)
        {

            var currentUser = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
            userParams.CurrentUserName = currentUser.UserName;

            if (string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = currentUser.Gender == "male" ? "female" : "male";
            }
            var users = await _userRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(
                new PaginationHeader(users.CurrentPage,
                 users.PageSize, users.TotalCount, users.TotalPages));
            return Ok(users);
        }

        [Authorize(Roles = "Member")]
        [HttpGet("{id}")]
        public async Task<MemberDto> GetUser(int id)
        {
            var user = await _userRepository.GetMemberAsync(id);

            return user;
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {



            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            if (user is null) return NotFound();

            _mapper.Map(memberUpdateDto, user);
            if (await _userRepository.SaveAllAsync()) return NoContent();
            return BadRequest("Failed to update user");
        }
        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
            if (user == null)
            {
                return NotFound();
            }
            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };
            if (user.Photos.Count == 0) photo.IsMain = true;

            user.Photos.Add(photo);

            if (await _userRepository.SaveAllAsync()) return _mapper.Map<PhotoDto>(photo);

            return BadRequest("problem adding photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {

            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            if (user is null)
            {
                return NotFound();
            }
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo == null)
            {
                return NotFound();
            }
            if (photo.IsMain)
            {
                return BadRequest("this is alread your main photo");
            }
            var currentMember = user.Photos.FirstOrDefault(photo => photo.IsMain);
            if (currentMember is not null)
            {
                currentMember.IsMain = false;

            }
            photo.IsMain = true;
            if (await _userRepository.SaveAllAsync()) return NotFound();

            return BadRequest("Problem setting the main photo");
        }
    }
}