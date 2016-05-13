using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DocumentOCR;
using MYOB.DAL;
using Online.PP.Service.Data.Entities;
using Online.PracticePortal.Contract.Contacts;
using Online.PracticePortal.Contract.Documents;
using WKUK.CCH.Document.DocMgmt.DocManager;
using WKUK.CCH.Document.DocMgmt.Entities;
using WKUK.CCH.Document.DocMgmt.Gateway;
using DocumentType = Online.PracticePortal.Contract.Documents.DocumentType;

namespace Online.PP.Service.Data.Document
{
    class DocManagerWrapper : IDocManagerWrapper
    {
        /// <summary>
        /// Get the list of documents associated with a contact ID
        /// </summary>
        /// <param name="employeeId">Identifier of the employee on behalf of whom the call is made.</param>
        /// <param name="contactId">Identifier of the contact for which documents are requested.</param>
        /// <returns></returns>
        public IEnumerable<DocumentSummary> GetDocumentsByContact(int employeeId, int contactId)
        {
            DAL dal = null;
            var documentManager = GetDocManager(employeeId, out dal);
            var documents = documentManager.RetrieveDocumentListForContact(contactId,employeeId)
                .Where(d => !d.IsDeleted)
                .Select(d => new DocumentSummary()
                {
                    DocumentId = d.DocumentId,
                    Name = d.Name,
                    LastModifiedDate = d.LastModifiedDate,
                    DocumentDate = d.DocumentDate
                });
            return documents;
        }

        public DocumentFileInfo GetDocumentFile(int emplpyeeId, int contactId, int documentId)
        {
            DAL dal;
            var docManager = GetDocManager(emplpyeeId, out dal);
            var docGateway = new DocumentGateway();

            var doc = docGateway.DocumentListForIEF(dal, documentId)[0];
            
            var docTempFile = Path.GetTempFileName();
            doc.CheckOutPath = docTempFile;

            docManager.GetDocumentFromFileStore(doc, CommonEnums.DocumentVersionControlStatus.CheckedOutByAnotherUser);
            return
               new DocumentFileInfo()
               {
                   LocalPath = doc.LocalPath,
                   FileName = doc.Name
               };
        }

        public Task<IEnumerable<Assignment>> GetAssignmentsForClient(int employeeId, int clientId)
        {
            DAL dal;
            var docManager = GetDocManager(employeeId, out dal);
          
            var assignemntDataSet = docManager.RetrieveAssignmentsForClient(clientId);

            var assignments = new List<Assignment>();
            foreach (DataTable table in assignemntDataSet.Tables)
            {
                assignments.AddRange(table.Rows.Cast<object>()
                    .Select((t, i) => 
                        new Assignment()
                        {
                            AssignmentId = Convert.ToInt16(assignemntDataSet.Tables[0].Rows[i][0]), 
                            AssignmentName = assignemntDataSet.Tables[0].Rows[i][1].ToString(), 
                            AssignmentTemplateId = (assignemntDataSet.Tables[0].Rows[i][3] != DBNull.Value ) ? Convert.ToInt16(assignemntDataSet.Tables[0].Rows[i][3]):0
                        }));
            }
            return Task.FromResult<IEnumerable<Assignment>>(assignments.OrderBy(a => a.AssignmentName));
        }

        public Task<IEnumerable<PracticePortal.Contract.Documents.DocumentType>> GetDocumentTypes(int employeeId, int documentLibraryId)
        {
            var libraries = GetDocumentLibraries(employeeId).Result;

            //all document types
            if (documentLibraryId == -1)
            {
                List<DocumentType> allDocumentTypes = new List<DocumentType>();
                foreach (var library in libraries)
                {
                    documentLibraryId = library.DocumentLibraryId;

                    var docTypes = RetrieveDocumentTypesbyLibrary(employeeId, documentLibraryId);
                    allDocumentTypes = allDocumentTypes.Union(docTypes).ToList();
                }
                return Task.FromResult(allDocumentTypes.OrderBy(d=>d.DocumentTypeName).GroupBy(x => x.DocumentTypeId).Select(x => x.First()));
            }

            //document types for clients/contacts
            if (documentLibraryId == 0)
            {                
                var clientLibrary = libraries.FirstOrDefault(l => l.DocumentLibraryName.Equals("Client"));
                documentLibraryId = clientLibrary.DocumentLibraryId;
            }

            var documentTypes = RetrieveDocumentTypesbyLibrary(employeeId, documentLibraryId);
            return Task.FromResult(documentTypes.GroupBy(x => x.DocumentTypeId).Select(x => x.First()));
        }

        private List<DocumentType> RetrieveDocumentTypesbyLibrary(int employeeId, int documentLibraryId)
        {
            DAL dal;
            var docManager = GetDocManager(employeeId, out dal);

            var documentTypes = new List<PracticePortal.Contract.Documents.DocumentType>();
            var typesDataSet = docManager.RetriveDocumentTypesForLibrary(documentLibraryId);

            documentTypes.AddRange(typesDataSet.Tables[1].Rows.Cast<object>()
                .Select((t, i) =>
                    new Online.PracticePortal.Contract.Documents.DocumentType()
                    {
                        DocumentTypeId = Convert.ToInt16(typesDataSet.Tables[1].Rows[i][0]),
                        DocumentTypeName = typesDataSet.Tables[1].Rows[i][1].ToString(),
                    }));
            return documentTypes;
        }

        public Task<IEnumerable<PracticePortal.Contract.Documents.DocumentLibrary>> GetDocumentLibraries(int employeeId)
        {
            DAL dal;
            var docManager = GetDocManager(employeeId, out dal);

            //[DM_MaintenanceDocumentLibrary_Retrieve] 
            var librariesDataSet = docManager.RetrieveDocumentLibraries();
            
            var libraries = new List<PracticePortal.Contract.Documents.DocumentLibrary>();
            foreach (DataTable table in librariesDataSet.Tables)
            {
                libraries.AddRange(table.Rows.Cast<object>()
                    .Select((t, i) =>
                        new PracticePortal.Contract.Documents.DocumentLibrary()
                        {
                            DocumentLibraryId = Convert.ToInt16(librariesDataSet.Tables[0].Rows[i][0]),
                            DocumentLibraryName = librariesDataSet.Tables[0].Rows[i][2].ToString(),
                        }));
                }
            return Task.FromResult<IEnumerable<PracticePortal.Contract.Documents.DocumentLibrary>>(libraries.OrderBy(l => l.DocumentLibraryName));
        }

        public bool UploadImage(string path, int employeeId)
        {
            var docPhotoHelper = new ScannedPhotoHelper(GetDal(), employeeId);
            docPhotoHelper.UploadPhoto(path, 747, 3, 2);//3,2

            return true;
        }

        #region private functionality

        private DocManager GetDocManager(int employeeId, out DAL dal)
        {
            dal = GetDal();
            var documentManager = new DocManager(dal, employeeId);
            return documentManager;
        }

        private DAL GetDal()
        {
            var lookupXmlPath = GetLookupXmlPathRelativeToExecutingAssembly();
            return new DAL(lookupXmlPath, ConfigurationManager.AppSettings["LookupXmlKey"], true, true);
        }

        private string GetLookupXmlPathRelativeToExecutingAssembly()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.Combine(Path.GetDirectoryName(path), "Lookup.xml");
        }
        #endregion private functionality
    }
}
