using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentOCR
{
    public class UploadPhotoResult
    {
        public int? PhotoDocId { get; set; }

        public int? OCRDocId { get; set; }

        public string Message { get; set; }

        public Exception Exception { get; set; }
    }
}
