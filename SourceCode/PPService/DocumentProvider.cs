using System.Drawing;
using System.IO;
using System.Xml.Linq;
using DocumentOCR;
using MYOB.Central.WS.BusinessLogic.List;
using MYOB.DAL;
using Online.Dal;
using Online.Dal.Extensions;
using Online.PP.Service.Data.Central;
using Online.PP.Service.Data.Document;
using Online.PP.Service.Data.Entities;
using Online.PP.Storage;
using Online.PracticePortal.Contract.Contacts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Online.PP.Service.Data.Portal;
using Online.PracticePortal.Contract.Documents;

namespace Online.PP.Service.Data.Repositories
{

    public class DocumentProvider : IDocumentProvider
    {
        #region Constants
        public static class Procedures
        {
            public const string DM_SH_StageOneFilter = "DM_SH_StageOneFilter";
            public const string DM_SH_GetDocsMetadata = "DM_SH_GetDocsMetadata";
            public const string DM_Document_ListByContact = "DM_Document_ListByContact";
            public const string PP_ContactDocuments_GetByContactId = "PP.ContactDocuments_GetByContactId";
        }

        public static class Parameters
        {
            public const string ExtraFieldIds = "ExtraFieldIds";
            public const string RequestedTableJoins = "RequestedTableJoins";
            public const string DynamicWhere = "DynamicWhere";
            public const string MaxDocsCount = "MaxDocsCount";
            public const string ContactId = "ContactId";
            public const string AssignmentId = "AssignmentId";
            public const string LoggedInEmployeeId = "LoggedInEmployeeId";
            public const string OrderByXml = "OrderByXml";
            public const string RetrieveOrderData = "RetreiveOrderData";
            public const string ShowDeletedRecords = "ShowDeletedRecords";
            public const string EnableDiagnostic = "EnableDiagnostic";
            public const string XmlDocIds = "XmlDocIds";
            public const string ContactIdParam = "contactId";
            public const string EmployeeIdParam = "employeeId";
        }
        #endregion

        #region Stored Procedure Parameters
        // parameters for DM_SH_StageOneFilter stored procedure
        private const string ExtraFieldIds = "<ExtraFieldIds />";
        private const string RequestedTableJoins = "d,DV";                          // aliases for DB tables : d - DocumentHeader table, DV - DocumentVersion table
        private string DynamicWhere = " d.Deleted = 0 AND d.Archived = 0";    // query to filter documents (filter conditions)
        private int MaxDocsCount = 200;                                       // max number of retruned documents
        private const int ContactId = 0;                                            // if you want global documents for logged in employee --> contactId = 0
        // if you want documents for a specific employee --> contactId = employeeId
        private const int AssignmentId = 0;

        // OBC --> Order by Column, the column that the documents will be sorted (in this case Documentid)
        private const string OrderByXml = "<?xml version='1.0' encoding='utf-16'?><OBCC><OBC key='DocumentId' name='d.DocumentId' type='Int' aggr='' desc='False' /></OBCC>";

        private const int RetrieveOrderData = 0;                                    // if 1, returns also the name of the column used for sorting
        private const int ShowDeletedRecords = 0;                                   // if 1, returns also the destroyed documents 
        private const int EnableDiagnostic = 0;                                     // if 1, show what script runs when you do a search     


        // local parameters for DM_SH_StageOneFilter stored procedure (for a specific client/contact)
        //private const string LocalDynamicWhere = "";
        private const string LocalRequestedTableJoins = "a,dc,d,DV";
        const string LocalOrderByXml = "<?xml version='1.0' encoding='utf-16'?><OBCC><OBC key='DefaultSortColumn_LastModified' name='DV.DateCreated' type='DateTime' aggr='' desc='True'/></OBCC>";
        #endregion

        private readonly IDocManagerWrapper _docManager;
        private readonly ICentralWrapper _centralWrapper;
        private readonly IAsyncDal _asyncDal;
        private readonly IPortalWrapper _portalWrapper;
        private int docCount;

        public DocumentProvider(IDocManagerWrapper docManager, IAsyncDal asyncDal, IPortalWrapper portalWrapper, ICentralWrapper centralWrapper)
        {
            _asyncDal = asyncDal;
            _docManager = docManager;
            _portalWrapper = portalWrapper;
            _centralWrapper = centralWrapper;
        }

        #region IDocumentProvider Members
        /// <summary>
        /// Retrieves the number of documents associated with the contact specified by <paramref name="contactId"/> 
        /// </summary>
        /// <param name="employeeId">Identifier of the employee</param>
        /// <param name="contactId">Identifier of the contact</param>
        /// <returns>Count of documents associated with a particular contact.</returns>
        public async Task<int> CountDocumentsForContact(int employeeId, int contactId)
        {
            MaxDocsCount = 0;
            // get the Ids for the client's local documents
            var documentsIds = await GetDocumentsIdList(employeeId, contactId);

            return docCount;
        }

        /// <summary>
        /// Retrieves the documents associated with the contact specified by <paramref name="contactId"/> 
        /// </summary>
        /// <param name="employeeId">Identifier of the employee</param>
        /// <param name="contactId">Identifier of the contact</param>
        /// <param name="contactType">Type of contact</param>
        /// <param name="take">Use for pagination, represent number of documents for one page</param>
        /// <param name="skip">Use for pagination, number of documents that are skiped</param>
        /// <returns>Documents associated with a particular contact.</returns>
        public async Task<ContactDocumentContract> GetDocuments(int employeeId, int contactId, int contactType, int take, int skip, string searchOption)
        {
            if (searchOption != null)
            {
                UpdateSearchOptions(searchOption);
            }

            // get the Ids for the client's local documents
            var documentsIds = await GetDocumentsIdList(employeeId, contactId);

            // get the metadata for the documents (based on documentsIds list)
            var documents = await GetDocumentsMetadataBasedOnDocumentsIdList(documentsIds, employeeId);

            // convert from GlobalDocuments list to DocumentsSummary list of objects (because DocumentsService(and cilent side) works with DocumentSummary objects

            List<DocumentSummary> docSummaryObjectsList;
            if (_centralWrapper.HasSecurityPermissions(employeeId, contactType))
            {
                docSummaryObjectsList = documents.Select(doc =>
                    new DocumentSummary
                    {
                        DocumentId = doc.DocumentId,
                        Name = doc.Name,
                        LastModifiedDate = doc.LastModifiedDate,
                        DocumentDate = doc.DocumentDate
                    }).ToList();
            }
            else
            {
                docSummaryObjectsList = null;
            }

            // descending sort by LastModifiedDate, pagination
            return new ContactDocumentContract()
            {
                Documents = (docSummaryObjectsList == null) ? null : docSummaryObjectsList.OrderByDescending(x => x.LastModifiedDate).Skip(skip).Take(take),
                TotalDocumentCount = docCount
            };
        }

        private void UpdateSearchOptions(string searchOption)
        {
            var query = "";
            var searchModel = JsonConvert.DeserializeObject<DocumentSearchModel>(searchOption);

            const string dateOptionFormat = @" AND CONVERT(DATETIME, DATEDIFF(dd, 0, d.DocumentDate), 1)  {0} '{1}'";
            const string datesOptionFormat = @" AND CONVERT(DATETIME, DATEDIFF(dd, 0, d.DocumentDate), 1)  between '{0}'  AND  '{1}'";
            const string assignmentOptionFormat = @" AND a.AssignmentId = {0}";
            const string docTypeOptionFormat = @" AND d.DocumentTypeId = {0}";
            const string libraryOptionFormat = @" AND d.DocumentLibraryId = {0}";
            const string textOptionFormat = @" AND (d.Name LIKE '%{0}%' OR d.Description LIKE '%{0}%')";

            var isDateBetweenRange = searchModel.StartDate != null && searchModel.EndDate != null;
            query += (isDateBetweenRange) ?
                        string.Format(datesOptionFormat, GetDateFormat(searchModel.StartDate), GetDateFormat(searchModel.EndDate)) :
                     (searchModel.StartDate != null) ?
                        string.Format(dateOptionFormat, ">=", GetDateFormat(searchModel.StartDate)) :
                     (searchModel.EndDate != null) ?
                        string.Format(dateOptionFormat, "<=", GetDateFormat(searchModel.EndDate)) : "";

            if (searchModel.AssignmentId > 0)
            {
                query += string.Format(assignmentOptionFormat, searchModel.AssignmentId);
            }
            if (searchModel.DocumentTypeId > 0)
            {
                query += string.Format(docTypeOptionFormat, searchModel.DocumentTypeId);
            }
            if (searchModel.DocumentLibraryId > 0)
            {
                query += string.Format(libraryOptionFormat, searchModel.DocumentLibraryId);
            }
            if (searchModel.Text != null)
            {
                query += string.Format(textOptionFormat, searchModel.Text);
            }

            DynamicWhere += query;
        }

        public async Task<GlobalDocumentContract> GetEmployeeGlobalDocuments(int employeeId, int take, int skip, string searchOption)
        {
            if (searchOption != null)
            {
                UpdateSearchOptions(searchOption);
            }

            // get the Ids for the documents
            var documentsIds = await GetDocumentsIdList(employeeId, ContactId);

            // get the metadata for the documents (based on documentsIds list)
            var documents = await GetDocumentsMetadataBasedOnDocumentsIdList(documentsIds, employeeId);

            var libraries = await _docManager.GetDocumentLibraries(employeeId);

            return new GlobalDocumentContract()
            {
                Documents = documents.Skip(skip).Take(take),
                DocumentLibraries = libraries
            };
        }

        public async Task<IEnumerable<FolderSummary>> GetFoldersAsync(int employeeId)
        {
            var folderList = await _portalWrapper.GetFolders(employeeId);
            return UpdateDisplayFolderName(folderList);
        }

        public DocumentUploadResponse UploadFile(string sas, string practiceGuid, string userGuid, int documentId, int emplpyeeId, int contactId)
        {

            var fileInfo = _docManager.GetDocumentFile(emplpyeeId, contactId, documentId);

            //TODO : GET FILE NAME
            var docStore = new DocumentsStorage();
            var response = docStore.UploadDocument(fileInfo, practiceGuid, userGuid, sas);

            //compose response
            return response;
        }

        public Task<IEnumerable<Assignment>> GetAssignmentForClient(int employeeId, int contactId)
        {
            int clientId = _centralWrapper.GetClientId(contactId);
            return _docManager.GetAssignmentsForClient(employeeId, clientId);
        }

        public Task<IEnumerable<DocumentType>> GetDocumentTypes(int employeeId, int documentLibraryId)
        {
            return _docManager.GetDocumentTypes(employeeId, documentLibraryId);
        }

        public async Task<IEnumerable<GlobalDocumentSummary>> GetDocumentsListForContact(int employeeId, int contactId, int contactType, int take, int skip)
        {
            var documents = await _asyncDal.ExecuteSpReturnMappedDataAsync<GlobalDocumentSummary>(Procedures.DM_Document_ListByContact,
                employeeId.ToAsyncDalInParmEx(Parameters.EmployeeIdParam),
                contactId.ToAsyncDalInParmEx(Parameters.ContactIdParam));

            var hasFullPermissions = _centralWrapper.HasSecurityPermissions(employeeId, contactType) || documents.Any();
            return hasFullPermissions ? documents.Skip(skip).Take(take).AsEnumerable() : null;
        }

        public bool PostImageEmployeeGlobalDocuments(int employeeId, string imageString)
        {
            string path = "D://temp//PhotoFromMobile" + DateTime.Now.Hour + DateTime.Now.Minute +
                          ".tiff";
            SaveByteArrayAsImage(path, imageString);
            return _docManager.UploadImage(path, employeeId);
        }

        #endregion


        #region Private Methods
        private void SaveByteArrayAsImage(string fullOutputPath, string base64String)
        {
            byte[] bytes = Convert.FromBase64String(base64String);

            Image image;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                image = Image.FromStream(ms);

                if (File.Exists(fullOutputPath))
                    File.Delete(fullOutputPath);

                image.Save(fullOutputPath, System.Drawing.Imaging.ImageFormat.Tiff);
            }
        }

        private async Task<IEnumerable<DocumentIdHelper>> GetDocumentsIdList(int employeeId, int contactId)
        {
            IEnumerable<DocumentIdHelper> documentsIds;

            if (contactId != 0)     // if contactId = 0 ==> global documents, contactId != 0 ==> local client's/contact's documents 
            {
                // get the Ids for the local documents (for a specific client)
                var context =
                    await _asyncDal.ExecuteSpReturnMappingContextAsync(Procedures.PP_ContactDocuments_GetByContactId,
                        ExtraFieldIds.ToAsyncDalInParmEx(Parameters.ExtraFieldIds),
                        LocalRequestedTableJoins.ToAsyncDalInParmEx(Parameters.RequestedTableJoins),
                        DynamicWhere.ToAsyncDalInParmEx(Parameters.DynamicWhere),
                        MaxDocsCount.ToAsyncDalInParmEx(Parameters.MaxDocsCount),
                        contactId.ToAsyncDalInParmEx(Parameters.ContactId),
                        AssignmentId.ToAsyncDalInParmEx(Parameters.AssignmentId),
                        employeeId.ToAsyncDalInParmEx(Parameters.LoggedInEmployeeId),
                        LocalOrderByXml.ToAsyncDalInParmEx(Parameters.OrderByXml),
                        RetrieveOrderData.ToAsyncDalInParmEx(Parameters.RetrieveOrderData),
                        ShowDeletedRecords.ToAsyncDalInParmEx(Parameters.ShowDeletedRecords),
                        EnableDiagnostic.ToAsyncDalInParmEx(Parameters.EnableDiagnostic));

                documentsIds = new List<DocumentIdHelper>();
                List<DocumentCount> docCountList = new List<DocumentCount>();

                await context.MapAsync(docCountList);
                docCount = docCountList.First().DocCount;

                if (docCount > 0 && MaxDocsCount > 0)
                    await context.MapAsync((List<DocumentIdHelper>)documentsIds);

            }
            else
            {
                // get the Ids for the global documents (all the documents for the logged in employee)
                documentsIds = await _asyncDal.ExecuteSpReturnMappedDataAsync<DocumentIdHelper>(Procedures.DM_SH_StageOneFilter,
                    ExtraFieldIds.ToAsyncDalInParmEx(Parameters.ExtraFieldIds),
                    RequestedTableJoins.ToAsyncDalInParmEx(Parameters.RequestedTableJoins),
                    DynamicWhere.ToAsyncDalInParmEx(Parameters.DynamicWhere),
                    MaxDocsCount.ToAsyncDalInParmEx(Parameters.MaxDocsCount),
                    contactId.ToAsyncDalInParmEx(Parameters.ContactId),
                    AssignmentId.ToAsyncDalInParmEx(Parameters.AssignmentId),
                    employeeId.ToAsyncDalInParmEx(Parameters.LoggedInEmployeeId),
                    OrderByXml.ToAsyncDalInParmEx(Parameters.OrderByXml),
                    RetrieveOrderData.ToAsyncDalInParmEx(Parameters.RetrieveOrderData),
                    ShowDeletedRecords.ToAsyncDalInParmEx(Parameters.ShowDeletedRecords),
                    EnableDiagnostic.ToAsyncDalInParmEx(Parameters.EnableDiagnostic));
            }

            return documentsIds;
        }

        private async Task<List<GlobalDocumentSummary>> GetDocumentsMetadataBasedOnDocumentsIdList(IEnumerable<DocumentIdHelper> documentsIds, int employeeId)
        {
            // get the metadata for the documents
            var xmlDocIds = new XElement("docs");

            foreach (var doc in documentsIds)
                xmlDocIds.Add(new XElement("d",
                    new XAttribute("i", doc.DocId),
                    new XAttribute("v", doc.DocVersionId)));

            var xmlDocs = xmlDocIds.ToString();

            var documentsMetadata = await _asyncDal.ExecuteSpReturnMappedDataAsync<GlobalDocumentSummary>(Procedures.DM_SH_GetDocsMetadata,
                xmlDocs.ToAsyncDalInParmEx(Parameters.XmlDocIds),
                employeeId.ToAsyncDalInParmEx(Parameters.LoggedInEmployeeId));

            return documentsMetadata.ToList();
        }

        private static IEnumerable<FolderSummary> UpdateDisplayFolderName(IEnumerable<FolderSummary> folderList)
        {
            var updatedFolderList = folderList as IList<FolderSummary> ?? folderList.ToList();
            foreach (var folder in updatedFolderList)
            {
                if (String.IsNullOrEmpty(folder.DisplayName))
                {
                    folder.DisplayName = GetFolderDisplayName(folder, updatedFolderList);

                }
            }
            return updatedFolderList.OrderBy(f => f.DisplayName);
        }

        private static string GetFolderDisplayName(FolderSummary folder, IEnumerable<FolderSummary> folderList)
        {
            var updatedFolderList = folderList as IList<FolderSummary> ?? folderList.ToList();
            if (folder.ParentFolderGuid.HasValue)
            {
                var parentFolder = updatedFolderList.First(f => f.FolderGuid.Equals(folder.ParentFolderGuid.Value));
                return GetFolderDisplayName(parentFolder, updatedFolderList) + @"\" + folder.Name;
            }
            return @"\" + folder.Name;
        }

        private static string GetDateFormat(string date)
        {
            var dateTime = DateTime.ParseExact(date, "MM/dd/yyyy", CultureInfo.InvariantCulture);
            return dateTime.ToString("yyyyMMdd");
        }
        #endregion
    }
}
