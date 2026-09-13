using Jenwa.Inquiry.Models;
using Jenwa.Inquiry.Services;
using Xunit;

namespace Jenwa.Inquiry.Tests;

public class InquiryValidatorTests
{
    static InquiryRequest Valid() => new()
    {
        Name = "曾先生",
        Phone = "0912-345-678",
        Email = "rex@example.com",
        Location = "台中市新社區",
        Purpose = "日常住宅",
        Message = "預計 20 坪，兩人使用。",
        Elapsed = 12_000,
    };

    [Fact]
    public void Accepts_a_fully_filled_form()
        => Assert.Equal(ValidationOutcome.Ok, InquiryValidator.Validate(Valid()).Outcome);

    [Fact]
    public void Accepts_a_form_with_only_the_required_fields()
    {
        var request = new InquiryRequest { Name = "王小姐", Phone = "0423456789", Location = "南投縣", Elapsed = 5000 };
        Assert.Equal(ValidationOutcome.Ok, InquiryValidator.Validate(request).Outcome);
    }

    [Fact]
    public void Drops_a_submission_that_filled_the_honeypot()
    {
        var request = Valid();
        request.CompanyUrl = "http://spam.example";
        Assert.Equal(ValidationOutcome.SilentDrop, InquiryValidator.Validate(request).Outcome);
    }

    [Fact]
    public void Drops_a_submission_that_arrived_too_fast()
    {
        var request = Valid();
        request.Elapsed = 500;
        Assert.Equal(ValidationOutcome.SilentDrop, InquiryValidator.Validate(request).Outcome);
    }

    [Fact]
    public void Allows_a_submission_with_no_timing_information()
    {
        // A cached older app.js sends no elapsed value; that must not lock people out.
        var request = Valid();
        request.Elapsed = 0;
        Assert.Equal(ValidationOutcome.Ok, InquiryValidator.Validate(request).Outcome);
    }

    [Fact]
    public void Reports_an_unparsable_body()
        => Assert.Equal(ValidationOutcome.Invalid, InquiryValidator.Validate(null).Outcome);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Requires_a_name(string? name)
    {
        var request = Valid();
        request.Name = name;
        var result = InquiryValidator.Validate(request);
        Assert.Equal(ValidationOutcome.Invalid, result.Outcome);
        Assert.Equal("name", result.Reason);
    }

    [Theory]
    [InlineData("12345")]          // too short
    [InlineData("abc-def-ghij")]   // letters are not allowed by the form pattern
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_a_phone_the_form_pattern_would_reject(string? phone)
    {
        var request = Valid();
        request.Phone = phone;
        Assert.Equal("phone", InquiryValidator.Validate(request).Reason);
    }

    [Theory]
    [InlineData("0912345678")]
    [InlineData("04 2345 6789")]
    [InlineData("+886 912 345 678")]
    [InlineData("(04)2345-6789")]
    public void Accepts_the_phone_formats_people_actually_type(string phone)
    {
        var request = Valid();
        request.Phone = phone;
        Assert.Equal(ValidationOutcome.Ok, InquiryValidator.Validate(request).Outcome);
    }

    [Fact]
    public void Requires_a_location()
    {
        var request = Valid();
        request.Location = " ";
        Assert.Equal("location", InquiryValidator.Validate(request).Reason);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("rex@localhost")]
    [InlineData("rex @example.com")]
    public void Rejects_a_malformed_email(string email)
    {
        var request = Valid();
        request.Email = email;
        Assert.Equal("email", InquiryValidator.Validate(request).Reason);
    }

    [Fact]
    public void Treats_a_blank_email_as_not_provided()
    {
        var request = Valid();
        request.Email = "  ";
        Assert.Equal(ValidationOutcome.Ok, InquiryValidator.Validate(request).Outcome);
    }

    [Fact]
    public void Rejects_a_message_over_the_form_limit()
    {
        var request = Valid();
        request.Message = new string('長', 3001);
        Assert.Equal("message", InquiryValidator.Validate(request).Reason);
    }

    [Fact]
    public void Accepts_a_message_at_the_form_limit()
    {
        var request = Valid();
        request.Message = new string('長', 3000);
        Assert.Equal(ValidationOutcome.Ok, InquiryValidator.Validate(request).Outcome);
    }

    [Fact]
    public void Builds_a_message_in_taipei_time_within_the_line_text_limit()
    {
        var request = Valid();
        request.Message = new string('長', 3000);
        var text = InquiryValidator.BuildMessage(request, new DateTimeOffset(2026, 9, 12, 3, 4, 0, TimeSpan.Zero));

        Assert.StartsWith("【易立構】新場勘諮詢", text);
        Assert.Contains("姓名：曾先生", text);
        Assert.Contains("電話：0912-345-678", text);
        Assert.Contains("基地：台中市新社區", text);
        Assert.Contains("時間：2026/09/12 11:04（台北）", text); // UTC+8
        Assert.True(text.Length < 5000, $"LINE text messages cap at 5000 characters; was {text.Length}.");
    }

    [Fact]
    public void Shows_a_dash_for_the_optional_fields_left_empty()
    {
        var request = new InquiryRequest { Name = "王小姐", Phone = "0423456789", Location = "南投縣" };
        var text = InquiryValidator.BuildMessage(request, DateTimeOffset.UtcNow);
        Assert.Contains("信箱：—", text);
        Assert.Contains("需求：—", text);
    }

    [Theory]
    [InlineData("0912-345-678", "0912345678")]
    [InlineData("0912 345 678", "0912345678")]
    [InlineData("(04)2345-6789", "0423456789")]
    [InlineData(null, "")]
    public void Normalises_a_phone_to_digits_so_reformatting_cannot_bypass_the_throttle(string? input, string expected)
        => Assert.Equal(expected, InquiryValidator.NormalisePhone(input));
}
