using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using CSP.Core.Security.WebApi;
using Online.Core;
using Online.PracticePortal.Contract;
using Online.PracticePortal.Contract.Contacts;
using Online.PracticePortal.Contract.Documents;
using Online.PracticePortal.Contract.Services;
using Online.PracticePortal.Service.Client;

namespace Online.PracticePortal.Api.Controllers
{
    [RoutePrefix("api/practicePortal")]
    [ClaimsAuthorize(ValidScopes = AuthorizationScopes.PracticePortalAccess)]
    public class DocumentApiController : ApiController
    {
        //redirect to DocumentService

        private readonly ISessionContext _context;
        private readonly IServiceProxyFactory _relayServiceFactory;

        /// <summary>
        /// Create a new Documents controller.
        /// </summary>
        /// <param name="context">Request specific session context.</param>
        /// <param name="relayServiceFactory">Relay factory used to obtain relay proxy instances that can be used to invoke relay service.</param>
        public DocumentApiController(ISessionContext context, IServiceProxyFactory relayServiceFactory)
        {
            _context = context;
            _relayServiceFactory = relayServiceFactory;
        }

        /// <summary>
        /// Retrieves the documents associated with the contact specified by <paramref name="contactId"/> 
        /// </summary>
        /// <param name="contactId">Identifier of the contact</param>
        /// <param name="contactType">Type of contact</param>
        /// <param name="take">Use for pagination, represent number of documents for one page</param>
        /// <param name="skip">Use for pagination, number of documents that are skiped</param>
        /// <returns>Redirect to DocumentService with the informations about the logged user</returns>
        [HttpGet]
        [Route("contacts/{contactId}/documents")]
        public async Task<object> GetDocuments(int contactId, int contactType, int take, int skip, string searchOption)
        {
            var identity = _context.LoggedInIdentity;
            var proxy = _relayServiceFactory.GetProxy<IDocumentService>(Guid.Parse(identity.PracticeGuid));

            return await proxy.GetDocuments(new IdentityContract()
            {
                UserId = identity.Name,
                PracticeGuid = Guid.Parse(identity.PracticeGuid)
            }, contactId, contactType, take, skip, searchOption);
        }

        [HttpGet]
        [Route("contacts/{contactId}/attachment-documents")]
        public async Task<object> GetDocuments(int contactId, int contactType, int take, int skip)
        {
            var identity = _context.LoggedInIdentity;
            var proxy = _relayServiceFactory.GetProxy<IDocumentService>(Guid.Parse(identity.PracticeGuid));

            return await proxy.GetDocumentsListForContact(new IdentityContract()
            {
                UserId = identity.Name,
                PracticeGuid = Guid.Parse(identity.PracticeGuid)
            }, contactId, contactType, take, skip);
        }

        [HttpGet]
        [Route("documents")]
        public GlobalDocumentContract GetEmployeeGlobalDocuments(int take, int skip, string searchOption)
        {
            var identity = _context.LoggedInIdentity;
            var proxy = _relayServiceFactory.GetProxy<IDocumentService>(Guid.Parse(identity.PracticeGuid));

            return proxy.GetEmployeeGlobalDocuments(new IdentityContract()
            {
                UserId = identity.Name,
                PracticeGuid = Guid.Parse(identity.PracticeGuid)
            }, take, skip, searchOption);
        }

        [HttpGet]
        [Route("client-assignments")]
        public async Task<IEnumerable<Assignment>> GetAssignmentsForClient(int contactId)
        {
            var identity = _context.LoggedInIdentity;
            var proxy = _relayServiceFactory.GetProxy<IDocumentService>(Guid.Parse(identity.PracticeGuid));

            return await proxy.GetAssignmentsForClient(new IdentityContract()
            {
                UserId = identity.Name,
                PracticeGuid = Guid.Parse(identity.PracticeGuid)
            }, contactId);
        }

        [HttpGet]
        [Route("client-portal-folders")]
        public async Task<IEnumerable<FolderSummary>> GetFolders()
        {
            var identity = _context.LoggedInIdentity;
            var proxy = _relayServiceFactory.GetProxy<IDocumentService>(Guid.Parse(identity.PracticeGuid));

            return await proxy.GetFolders(new IdentityContract()
            {
                UserId = identity.Name,
                PracticeGuid = Guid.Parse(identity.PracticeGuid)
            });
        }

        [HttpGet]
        [Route("document-types")]
        public async Task<IEnumerable<DocumentType>> GetDocumentTypes(int documentLibraryId)
        {
            var identity = _context.LoggedInIdentity;
            var proxy = _relayServiceFactory.GetProxy<IDocumentService>(Guid.Parse(identity.PracticeGuid));

            return await proxy.GetDocumentTypes(new IdentityContract()
            {
                UserId = identity.Name,
                PracticeGuid = Guid.Parse(identity.PracticeGuid)
            }, documentLibraryId);
        }

        [HttpPost]
        [Route("documents")]
        public bool PostImageEmployeeGlobalDocuments(DocumentImage di)
        {
            var identity = _context.LoggedInIdentity;
            var proxy = _relayServiceFactory.GetProxy<IDocumentService>(Guid.Parse(identity.PracticeGuid));

            return proxy.PostImageEmployeeGlobalDocuments(new IdentityContract()
            {
                UserId = identity.Name,
                PracticeGuid = Guid.Parse(identity.PracticeGuid)
            }, di.ImageString);
        }
    }
}
