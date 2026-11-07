using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Cms
{
    public class ProductQuestionDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string AuthorName { get; set; } = "Khách hàng";
        public string Content { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool IsHidden { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProductAnswerDto> Answers { get; set; } = new();
    }

    public class ProductAnswerDto
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public int? UserId { get; set; }
        public string AuthorName { get; set; } = "Chuyên viên Dược Mỹ Phẩm Canmake Beauty";
        public string Content { get; set; } = string.Empty;
        public bool IsOfficial { get; set; } = false;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SubmitQuestionRequest
    {
        public int ProductId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? AuthorName { get; set; }
    }

    public class SubmitAnswerRequest
    {
        public int QuestionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsOfficial { get; set; } = false;
        public string? AuthorName { get; set; }
    }
}

