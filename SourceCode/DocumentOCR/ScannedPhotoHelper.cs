using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MYOB.DAL;
using WKUK.CCH.Document.DocMgmt.Entities;
using WKUK.CCH.Document.DocMgmt.DocManager;

namespace DocumentOCR
{
    /// <summary>
    /// 
    /// </summary>
    public class ScannedPhotoHelper
    {
        #region fields

        private MYOB.DAL.DAL _dal;
        private int _loggedEmployeeId;
        private DocManager _docMan;

        private int? _clientLibraryId;
        private short? _docTypeId;
        private int? _employeeContactId;

        private string _linkDocsRelationship = "Email/Attachment";
        private int? _linkDocsRelationshipId;

        #endregion

        public ScannedPhotoHelper(MYOB.DAL.DAL dal, int loggedEmployeeId)
        {
            _dal = dal;
            _loggedEmployeeId = loggedEmployeeId;
            _docMan = new DocManager(dal, loggedEmployeeId);
        }

        public bool UseBuiltinOcr { get; set; }

        public UploadPhotoResult UploadPhoto(string path, int toContactId, int? toAssignmentId, int? toJobId)
        {
            try
            {
                string dbNotSetupMsg = GetDbSettings();

                if (dbNotSetupMsg != null)
                    return new UploadPhotoResult() { Message = dbNotSetupMsg };

                if (UseBuiltinOcr)
                {
                    var result = new UploadPhotoResult();

                    // upload photo to DM
                    int photoDocId = UploadFile(path, (int)_clientLibraryId, CommonEnums.DocumentSource.Upload, (short)_docTypeId, (int)_employeeContactId, toContactId, toAssignmentId, toJobId, false);
                    result.PhotoDocId = photoDocId;
                    result.Message = string.Format("Uploaded photo docId: {0}\r\n", photoDocId);

                    // ocr the image
                    string ocrPath = GetPdfFilePath(path);

                    if (OCRWrapper.Instance.PerformOcr(path, ocrPath))
                    {
                        // upload the ocr
                        int ocrDocId = UploadFile(ocrPath, (int)_clientLibraryId, CommonEnums.DocumentSource.Upload, (short)_docTypeId, (int)_employeeContactId, toContactId, toAssignmentId, toJobId, false);
                        result.OCRDocId = ocrDocId;
                        result.Message += string.Format("Uploaded OCR docId: {0}\r\n", ocrDocId);

                        bool link = LinkDocuments("Email\\Attachment", photoDocId, ocrDocId);
                        if (link)
                            result.Message += "Documents have been linked";
                    }
                    else
                    {
                        result.Message += "OCR Exception " + OCRWrapper.Instance.LastOCRException.ToString();
                        result.Exception = OCRWrapper.Instance.LastOCRException;
                    }

                    return result;
                }
                else
                {
                    // upload the photo to DM with a Scan document source, and expect the autofill service to upload a pdf
                    var photoDocId = UploadFile(path, (int)_clientLibraryId, CommonEnums.DocumentSource.Scan, (short)_docTypeId, (int)_employeeContactId, toContactId, toAssignmentId, toJobId, true);

                    return new UploadPhotoResult() { Message = "Upload Successful.", PhotoDocId = photoDocId };
                }

            }
            catch (Exception ex)
            {
                return new UploadPhotoResult() { Message = ex.Message, Exception = ex };
            }
        }

        private string GetPdfFilePath(string originalPath)
        {
            string dirPath = Path.GetDirectoryName(originalPath);
            string pdfName = Path.GetFileNameWithoutExtension(originalPath) + ".pdf";

            return Path.Combine(dirPath, pdfName);
        }

        private string GetDbSettings()
        {
            if (_clientLibraryId == null)
            {
                var rows = _dal.GetDataset("SELECT * FROM dbo.DocumentLibrary WHERE DocumentLibraryTypeId = 1").Tables[0].Rows;

                if (rows.Count == 0)
                    return "There is no DM library of type Client";
                else
                    _clientLibraryId = (int)rows[0]["DocumentLibraryId"];
            }

            if (_docTypeId == null)
            {
                var rows = _dal.GetDataset("SELECT * FROM dbo.DocumentType WHERE Name = 'ScannedPhotos'").Tables[0].Rows;

                if (rows.Count == 0)
                    return "There is no DM document type called ScannedPhotos";
                else
                    _docTypeId = (short)rows[0]["DocumentTypeId"];
            }


            if (_employeeContactId == null)
            {
                var rows = _dal.GetDataset("SELECT * FROM dbo.Employee WHERE EmployeeId = " + _loggedEmployeeId).Tables[0].Rows;

                if (rows.Count == 0)
                    return "There is no employee with ID " + _loggedEmployeeId;
                else
                    _employeeContactId = (int)rows[0]["ContactId"];
            }

            if (_linkDocsRelationshipId == null)
            {
                var rows = _dal.GetDataset(string.Format("SELECT * FROM dbo.DocumentLinkType WHERE Name = '{0}' ", _linkDocsRelationship)).Tables[0].Rows;

                if (rows.Count == 0)
                    return "There is no DocumentLinkType with name " + _linkDocsRelationship;
                else
                    _linkDocsRelationshipId = (int)rows[0]["DocumentLinkTypeId"];
            }


            return null;
        }

        private int UploadFile(string filePath, int libraryId, CommonEnums.DocumentSource source, short documentTypeId, int createdByContactId, int? contactId, int? assignmentId, int? jobId, bool autoFill)
        {
            FileInfo fi = new FileInfo(filePath);

            var doc = new WKUK.CCH.Document.DocMgmt.Entities.Document();

            doc.Image = filePath;
            doc.LocalPath = filePath;
            doc.CheckOutPath = filePath;
            doc.FileExtension = Path.GetExtension(filePath);
            doc.Name = Path.GetFileName(filePath);

            doc.CreatedByContactId = createdByContactId;

            doc.SourceId = (int)source;
            doc.LibraryId = libraryId;
            doc.DocumentTypeId = documentTypeId;

            doc.CreatedDate = fi.CreationTime;
            doc.DocumentDate = fi.CreationTime;
            doc.UploadDate = DateTime.Now;
            doc.LeaveCheckedOut = true; // do not delete the file from disk
            doc.Description = ".";

            doc.AutoFill = autoFill;

            if (contactId != null)
            {
                doc.ContactID = (int)contactId;
                //doc.DocumentContacts.Add(new DocumentContact()
                //{
                //    ContactID = contactId
                //});
            }

            doc.AssignmentId = assignmentId;
            doc.AssignmentScheduleId = jobId;


            _docMan.UploadAddedDocuments(new DocumentCollection() { doc }, false, _loggedEmployeeId);

            return doc.DocumentId;
        }

        private bool LinkDocuments(string relationshipName, params int[] docIds)
        {
            LinkedDocumentsCollection linkedDocs = new LinkedDocumentsCollection();

            foreach (int docId in docIds)
            {
                LinkedDocument linkedDoc = new LinkedDocument();
                linkedDoc.DocumentId = docId;
                linkedDoc.Relationship = _linkDocsRelationship;
                linkedDoc.RelationshipId = (int)_linkDocsRelationshipId;
                linkedDoc.Status = CommonEnums.LinkedDocumentRecordStatus.Link;
                linkedDocs.Add(linkedDoc);
            }

            _docMan.AddLinkedDocumentsToDocument(linkedDocs);

            return true;
        }
    }
}
