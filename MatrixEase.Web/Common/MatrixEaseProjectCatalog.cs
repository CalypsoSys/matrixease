using System;
using System.Collections.Generic;

namespace MatrixEase.Web.Common
{
    public class MatrixEaseProjectDto
    {
        public string ProjectId { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string SheetType { get; set; }
        public DateTime Created { get; set; }
        public int MaxRows { get; set; }
        public int? TotalRows { get; set; }
        public string Status { get; set; }
        public bool IsPending { get; set; }
    }

    public class MatrixEaseProjectsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<MatrixEaseProjectDto> Projects { get; set; } = new List<MatrixEaseProjectDto>();
    }
}
