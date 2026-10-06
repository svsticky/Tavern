using System.ComponentModel.DataAnnotations;
using Backend.Controllers.DTOs;
using Backend.Models.Domain;

namespace Backend.Tests.Controllers.DTOs
{
    public class HouseNumberValidationTests
    {
        [Theory]
        [InlineData("1")]
        [InlineData("12")]
        [InlineData("123")]
        [InlineData("12a")]
        [InlineData("12 a")]
        [InlineData("12-a")]
        [InlineData("12bis")]
        [InlineData("12 bis")]
        [InlineData("12-bis")]
        [InlineData("12ABC")]
        [InlineData("12k7")]
        [InlineData("12 k7")]
        [InlineData("12-k7")]
        public void ValidHouseNumbers_PassValidation(string houseNumber)
        {
            Assert.True(IsValid(NewPostMemberDTO(houseNumber)));
            Assert.True(IsValid(NewMemberUpdateDTO(houseNumber)));
            Assert.True(IsValid(NewMember(houseNumber)));
        }

        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("01")]
        [InlineData("a12")]
        [InlineData("12abcd")]
        [InlineData("12--a")]
        [InlineData("12 -a")]
        [InlineData("bis")]
        [InlineData("12  a")]
        public void InvalidHouseNumbers_FailValidation(string houseNumber)
        {
            Assert.False(IsValid(NewPostMemberDTO(houseNumber)));
            Assert.False(IsValid(NewMemberUpdateDTO(houseNumber)));
            Assert.False(IsValid(NewMember(houseNumber)));
        }

        private static bool IsValid(object instance)
        {
            var context = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            return Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        }

        private static PostMemberDTO NewPostMemberDTO(string houseNumber) => new()
        {
            StudentNumber = "s1234567",
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            PhoneNumber = "0611223344",
            Street = "Kerkstraat",
            HouseNumber = houseNumber,
            PostalCode = "7511AA",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-25),
            PreferredLanguage = Language.EN
        };

        private static MemberUpdateDTO NewMemberUpdateDTO(string houseNumber) => new()
        {
            StudentNumber = "s1234567",
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            PhoneNumber = "0611223344",
            Street = "Kerkstraat",
            HouseNumber = houseNumber,
            PostalCode = "7511AA",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-25),
            PreferredLanguage = Language.EN
        };

        private static Member NewMember(string houseNumber) => new()
        {
            StudentNumber = "s1234567",
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            PhoneNumber = "0611223344",
            Street = "Kerkstraat",
            HouseNumber = houseNumber,
            PostalCode = "7511AA",
            City = "Enschede"
        };
    }
}
