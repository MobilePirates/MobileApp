using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.ObjectBuilder2;
using Online.Core.Util;
using Online.PP.Service.Data.Repositories;
using Online.PracticePortal.Contract;
using Online.PracticePortal.Contract.Contacts;
using Online.PracticePortal.Contract.Documents;
using Online.PracticePortal.Contract.Services;

namespace Online.PracticePortal.Service.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentProvider _documentProvider;
        private readonly IIdentityRepository _identityRepository;

        [ImportingConstructor]
        public DocumentService(IDocumentProvider documentProvider, IIdentityRepository identityRepository)
        {
            _documentProvider = documentProvider;
            _identityRepository = identityRepository;
        }

        /// <summary>
        /// Retrieves the documents associated with the contact specified by <paramref name="contactId"/> 
        /// </summary>
        /// <param name="identity">IdentityContract object that provide usefull informations about the user</param>
        /// <param name="contactId">Identifier of the contact</param>
        /// <param name="contactType">Type of contact</param>
        /// <param name="take">Use for pagination, represent number of documents for one page</param>
        /// <param name="skip">Use for pagination, number of documents that are skiped</param>
        /// <returns>Redirect to DocumentProvider</returns>
        public async Task<ContactDocumentContract> GetDocuments(IdentityContract identity, int contactId, int contactType, int take, int skip, string searchOption)
        {
            var user = _identityRepository.FindById(identity.UserId);
            var documentContract = await _documentProvider.GetDocuments(user.EmployeeId, contactId, contactType, take, skip, searchOption);

            if (documentContract.Documents != null)
            {
                documentContract.Documents.ForEach(d => d.DownloadUrl = string.Format("documents?documentId={0}&practiceGuid={1}&userGuid={2}&contactId={3}",
                                                                    d.DocumentId, identity.PracticeGuid, identity.UserId, contactId));
            }
            return documentContract;
        }

        public DocumentUploadResponse GetDocumentDetails(DocumentUploadRequest requestDetails)
        {
            var user = _identityRepository.FindById(requestDetails.UserGuid);
            return _documentProvider.UploadFile(requestDetails.SharedAccessSignature, requestDetails.PracticeGuid, requestDetails.UserGuid, requestDetails.DocumentId,
                                                requestDetails.ContactId, user.EmployeeId);
        }

        public GlobalDocumentContract GetEmployeeGlobalDocuments(IdentityContract identity, int take, int skip, string searchOption)
        {
            try
            {
                var user = _identityRepository.FindById(identity.UserId);
                return TaskServices.RunSynchronously(_documentProvider.GetEmployeeGlobalDocuments(user.EmployeeId, take, skip, searchOption));
            }
            catch (Exception ex)
            {

                Trace.TraceError("Failed to load  documents" + ex);
                throw;
            }
        }

        public Task<IEnumerable<FolderSummary>> GetFolders(IdentityContract identity)
        {
            var user = _identityRepository.FindById(identity.UserId);
            return _documentProvider.GetFoldersAsync(user.EmployeeId);
        }

        public Task<IEnumerable<Assignment>> GetAssignmentsForClient(IdentityContract identity, int contactId)
        {
            var user = _identityRepository.FindById(identity.UserId);
            return _documentProvider.GetAssignmentForClient(user.EmployeeId, contactId);
        }

        public Task<IEnumerable<DocumentType>> GetDocumentTypes(IdentityContract identity, int documentLibraryId)
        {
            var user = _identityRepository.FindById(identity.UserId);
            return _documentProvider.GetDocumentTypes(user.EmployeeId, documentLibraryId);
        }

        public Task<IEnumerable<GlobalDocumentSummary>> GetDocumentsListForContact(IdentityContract identity, int contactId, int contactType, int take, int skip)
        {
            var user = _identityRepository.FindById(identity.UserId);
            return _documentProvider.GetDocumentsListForContact(user.EmployeeId, contactId, contactType, take, skip);
        }

        public bool PostImageEmployeeGlobalDocuments(IdentityContract identity, string imageString)
        {
            var user = _identityRepository.FindById(identity.UserId);
            return _documentProvider.PostImageEmployeeGlobalDocuments(user.EmployeeId, imageString);
        }
    }
}
