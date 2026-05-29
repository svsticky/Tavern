using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Backend.Models;
using Xunit;

namespace Backend.Tests.Controllers
{
    public class DtoTests
    {
        [Fact]
        public void TestPostSpecificationQuestionDTO()
        {
            var dto = new PostSpecificationQuestionDTO
            {
                QuestionDutch = "Vraag",
                QuestionEnglish = "Question",
                Type = QuestionType.String,
                IsMandatory = true,
                IsPublic = true,
                Options = new List<string> { "Opt1" },
                ActivityId = 123
            };

            Assert.Equal("Vraag", dto.QuestionDutch);
            Assert.Equal("Question", dto.QuestionEnglish);
            Assert.Equal(QuestionType.String, dto.Type);
            Assert.True(dto.IsMandatory);
            Assert.True(dto.IsPublic);
            Assert.Contains("Opt1", dto.Options);
            Assert.Equal(123u, dto.ActivityId);
        }

        [Fact]
        public void TestPostEnrollmentResponseDTO()
        {
            var guid = Guid.NewGuid();
            var dto = new PostEnrollmentResponseDTO
            {
                ActivityId = 456,
                MemberId = guid
            };

            Assert.Equal(456u, dto.ActivityId);
            Assert.Equal(guid, dto.MemberId);
        }

        [Fact]
        public void TestMemberUpdateDTO()
        {
            var dob = DateTimeOffset.UtcNow.AddYears(-25);
            var dto = new MemberUpdateDTO
            {
                StudentNumber = "s9999999",
                FirstName = "Alice",
                LastName = "Smith",
                Email = "alice@example.com",
                PhoneNumber = "0611223344",
                Street = "Kerkstraat",
                HouseNumber = "45a",
                PostalCode = "7511AA",
                City = "Enschede",
                DateOfBirth = dob,
                ParentPhoneNumber = "0699887766",
                MailSubscriptions = 5,
                Notes = "Some note",
                PreferredLanguage = Language.EN,
                Gratie = true,
                LidVanVerdienste = true,
                EreLid = false,
                Begunstiger = true,
                Suspended = false
            };

            Assert.Equal("s9999999", dto.StudentNumber);
            Assert.Equal("Alice", dto.FirstName);
            Assert.Equal("Smith", dto.LastName);
            Assert.Equal("alice@example.com", dto.Email);
            Assert.Equal("0611223344", dto.PhoneNumber);
            Assert.Equal("Kerkstraat", dto.Street);
            Assert.Equal("45a", dto.HouseNumber);
            Assert.Equal("7511AA", dto.PostalCode);
            Assert.Equal("Enschede", dto.City);
            Assert.Equal(dob, dto.DateOfBirth);
            Assert.Equal("0699887766", dto.ParentPhoneNumber);
            Assert.Equal(5u, dto.MailSubscriptions);
            Assert.Equal("Some note", dto.Notes);
            Assert.Equal(Language.EN, dto.PreferredLanguage);
            Assert.True(dto.Gratie);
            Assert.True(dto.LidVanVerdienste);
            Assert.False(dto.EreLid);
            Assert.True(dto.Begunstiger);
            Assert.False(dto.Suspended);
        }

        [Fact]
        public void TestGroupResponseDTO()
        {
            var dto = new GroupResponseDTO
            {
                Id = 777,
                Name = "Gala",
                Active = true,
                Type = GroupType.Committee,
                GroupPicturePath = "images/gala.png"
            };

            Assert.Equal(777u, dto.Id);
            Assert.Equal("Gala", dto.Name);
            Assert.True(dto.Active);
            Assert.Equal(GroupType.Committee, dto.Type);
            Assert.Equal("images/gala.png", dto.GroupPicturePath);
        }
    }
}
