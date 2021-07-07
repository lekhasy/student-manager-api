using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace student_manager_api.Controllers
{
    public record RequestResult<T>(T? Data);

    public record RequestResult<T, MetaType>(T? Data, MetaType Meta);

    public record PagedMeta(int TotalItem);

    [ApiController]
    [Route("[controller]/[action]")]
    public class StudentController : ControllerBase
    {
        private static List<Student> students = new(MockData.GenerateData());

        public record AddStudentVM(DateTime AdmissionDate, string Name, Genders Gender, string PhoneNumber, IFormFile? ImageFile, DateTime Birthday);
        public record ModifyStudentVM(string Id, DateTime AdmissionDate, string Name, Genders Gender, string PhoneNumber, IFormFile? ImageFile, DateTime Birthday);

        [HttpGet]
        public RequestResult<IEnumerable<Student>, PagedMeta> GetStudents(string? search, int page, int pageSize)
        {
            search = string.IsNullOrEmpty(search) ? "" : search;

            var filteredResult = students.Where(s => s.PhoneNumber.Contains(search) || s.Name.Contains(search)).OrderBy(s => s.CreatedDate);

            return new RequestResult<IEnumerable<Student>, PagedMeta>(filteredResult.Skip((page - 1) * pageSize).Take(pageSize), new PagedMeta(filteredResult.Count()));
        }

        [HttpGet]
        public RequestResult<Student> GetStudent(string id)
        {
            var student = students.Find(s => s.Id == id);
            return new RequestResult<Student>(student);
        }

        [HttpPost]
        public async Task<RequestResult<Student>> AddStudent([FromForm] AddStudentVM viewModel)
        {
            var studentId = Guid.NewGuid().ToString();

            var fileName = "";

            if (viewModel.ImageFile != null)
            {
                fileName = await saveAvatarAsync(viewModel.ImageFile, studentId, null);
            }
            else
            {
                fileName = "default.jpeg";
            }

            Student newStudent = new Student(studentId, DateTime.UtcNow, viewModel.AdmissionDate, viewModel.Name, viewModel.Gender, "", viewModel.PhoneNumber, viewModel.Birthday) with { Id = studentId, Img = fileName };

            // clone and add new
            students = students.Select(t => t).ToList();
            students.Add(newStudent);

            return new RequestResult<Student>(newStudent);
        }

        [HttpPost]
        public async Task<ActionResult<RequestResult<Student>>> ModifyStudent([FromForm] ModifyStudentVM viewModel)
        {
            var studentId = viewModel.Id;

            var fileName = "";

            var matchStudent = students.Find(s => s.Id == studentId);

            if (matchStudent == null)
            {
                return NotFound();
            }

            if (viewModel.ImageFile != null)
            {
                fileName = await saveAvatarAsync(viewModel.ImageFile, studentId, matchStudent.Img);
            }
            else
            {
                fileName = "default.jpeg";
            }

            var newStudent = matchStudent
            with
            { Id = studentId, Img = fileName, Name = viewModel.Name, Birthday = viewModel.Birthday, Gender = viewModel.Gender, AdmissionDate = viewModel.AdmissionDate, PhoneNumber = viewModel.PhoneNumber };

            // clone and add new
            students = students.Select(t => t.Id == studentId ? newStudent : t).ToList();

            return new RequestResult<Student>(newStudent);
        }

        async Task<string> saveAvatarAsync(IFormFile file, string studentId, string? oldFileName)
        {
            var fileExtension = file.FileName.Split(".").Last();
            var fullPath = $"./uploadImg/{studentId}.{fileExtension}";
            if (oldFileName != null && oldFileName != "default.jpeg")
            {
                var oldFullPath = $"./uploadImg/{oldFileName}";
                System.IO.File.Delete(oldFullPath);
            }
            System.IO.File.Delete(fullPath);
            using (var fileStream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"{studentId}.{fileExtension}";
        }

    }

}