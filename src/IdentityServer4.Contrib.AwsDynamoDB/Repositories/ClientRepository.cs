/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Spudmash Media Pty Ltd. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.Logging;
using Amazon.DynamoDBv2;
using IdentityServer4.Contrib.AwsDynamoDB.Models;
using IdentityServer4.Contrib.AwsDynamoDB.Models.Extensions;

namespace IdentityServer4.Contrib.AwsDynamoDB.Repositories
{
    /// <summary>
    /// Client repository.
    /// </summary>
    public class ClientRepository : IClientStore
    {
        private readonly IDynamoDBContext dynamoDbContext;
        private readonly ILogger<ClientRepository> logger;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:IdentityServer4.Contrib.AwsDynamoDB.Repositories.ClientRepository"/> class.
        /// </summary>
        /// <param name="dynamoDbContext">Db context</param>
        /// <param name="logger">Logger.</param>
        public ClientRepository(IDynamoDBContext dynamoDbContext, ILogger<ClientRepository> logger)
        {
            this.dynamoDbContext = dynamoDbContext;
            this.logger = logger;
        }

        /// <summary>
        /// Finds the client by identifier async.
        /// </summary>
        /// <returns>The client by identifier async.</returns>
        /// <param name="clientId">Client identifier.</param>
        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            if (string.IsNullOrEmpty(clientId)) return null;

            Client response = null;
            try
            {
                var batch = dynamoDbContext.QueryAsync<ClientDynamoDB>(clientId);

                var dataset = await batch.GetRemainingAsync();

                response = dataset?.First().GetClient();
            }
            catch (Exception ex)
            {
                logger.LogError(default(EventId), ex, "ClientRepository.FindClientByIdAsync failed with clientId {clientId}", clientId);
                throw;
            }

            return response;
        }

        /// <summary>
        /// Stores the client async.
        /// </summary>
        /// <returns>The client async.</returns>
        /// <param name="item">Item.</param>
        public async Task StoreClientAsync(Client item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            try
            {
                await dynamoDbContext.SaveAsync(item.GetClientDynamoDB());
            }
            catch (Exception ex)
            {
                logger.LogError(default(EventId), ex, "ClientRepository.StoreClientAsync failed with Client {item}", item);
                throw;
            }

            await Task.CompletedTask;
        }
    }
}
