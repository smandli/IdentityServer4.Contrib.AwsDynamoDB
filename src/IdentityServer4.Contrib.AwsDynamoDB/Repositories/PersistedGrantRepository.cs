/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Spudmash Media Pty Ltd. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using IdentityServer4.Contrib.AwsDynamoDB.Models;
using IdentityServer4.Contrib.AwsDynamoDB.Models.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Contrib.AwsDynamoDB.Repositories
{
    /// <summary>
    /// Persisted grant repository.
    /// </summary>
    public class PersistedGrantRepository : IPersistedGrantStore
    {
        private readonly IDynamoDBContext dynamoDbContext;
        private readonly ILogger<PersistedGrantRepository> logger;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:IdentityServer4.Contrib.AwsDynamoDB.Repositories.PersistedGrantRepository"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="dynamoDbContext">DB Context</param>
        public PersistedGrantRepository(IDynamoDBContext dynamoDbContext, DynamoDBContextConfig ddbConfig, ILogger<PersistedGrantRepository> logger)
        {
            this.dynamoDbContext = dynamoDbContext;
            this.logger = logger;
        }

        /// <summary>
        /// Gets all async.
        /// </summary>
        /// <returns>The all async.</returns>
        /// <param name="subjectId">Subject identifier.</param>
        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            if (string.IsNullOrEmpty(subjectId)) { return null; }

            IEnumerable<PersistedGrant> result = null;

            try
            {
                var obj = await dynamoDbContext.QueryAsync<PersistedGrantDynamoDB>(subjectId).GetRemainingAsync();

                result = obj.Select(item => item.GetPersistedGrant());
            }
            catch (Exception ex)
            {
                logger.LogError(default(EventId), ex, "PersistedGrantRepository.GetAsync failed with subjectId {subjectId}", subjectId);
                await Task.FromException(ex);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Gets the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="key">Key.</param>
        public async Task<PersistedGrant> GetAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) { return null; }

            PersistedGrant result = null;

            try
            {
                var data = await dynamoDbContext.QueryAsync<PersistedGrantDynamoDB>(key).GetRemainingAsync();
                if(data.Any()){
                    result = data.First().GetPersistedGrant();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default(EventId), ex, "PersistedGrantRepository.GetAsync failed with key {key}", key);
                await Task.FromException(ex);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Removes all async.
        /// 
        /// subjectId && clientId
        /// 
        /// see https://github.com/IdentityServer/IdentityServer4/blob/325ea0e67b1de2c836f26eb38436ff07177028d4/src/IdentityServer4/Stores/InMemory/InMemoryPersistedGrantStore.cs#L86
        /// 
        /// </summary>
        /// <returns>The all async.</returns>
        /// <param name="subjectId">Subject identifier.</param>
        /// <param name="clientId">Client identifier.</param>
        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(clientId))
            {
                await Task.FromException(new ArgumentNullException($"PersistedGrantRepository.RemoveAllAsync subjectId: {subjectId} And/Or clientId:{clientId} is null"));
                return;
            }

            List<ScanCondition> conditions = new List<ScanCondition>();
            conditions.Add(new ScanCondition(nameof(subjectId), ScanOperator.Equal, subjectId));
            conditions.Add(new ScanCondition(nameof(clientId), ScanOperator.Equal, clientId));

            try
            {
                var batch = dynamoDbContext.ScanAsync<PersistedGrantDynamoDB>(conditions);
                while (!batch.IsDone)
                {
                    var dataset = await batch.GetNextSetAsync();

                    if (dataset.Any())
                    {
                        dataset.ForEach(async item => await dynamoDbContext.DeleteAsync(item));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default(EventId), ex, "PersistedGrantRepository.RemoveAllAsync failed with subjectId: {subjectId}, clientId: {clientId}", subjectId, clientId);
                await Task.FromException(ex);
                return;
            }

            await Task.CompletedTask;
            return;
        }

        /// <summary>
        /// Removes all async.
        /// 
        /// subjectId && clientId && type
        /// 
        /// see https://github.com/IdentityServer/IdentityServer4/blob/325ea0e67b1de2c836f26eb38436ff07177028d4/src/IdentityServer4/Stores/InMemory/InMemoryPersistedGrantStore.cs#L110
        /// 
        /// </summary>
        /// <returns>The all async.</returns>
        /// <param name="subjectId">Subject identifier.</param>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="type">Type.</param>
        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(type))
            {
                await Task.FromException(new ArgumentNullException($"PersistedGrantRepository.RemoveAllAsync subjectId:{subjectId} And/Or clientId:{clientId} And/Or type:{type} is null"));
                return;
            }

            List<ScanCondition> conditions = new List<ScanCondition>();
            conditions.Add(new ScanCondition(nameof(subjectId), ScanOperator.Equal, subjectId));
            conditions.Add(new ScanCondition(nameof(clientId), ScanOperator.Equal, clientId));
            conditions.Add(new ScanCondition(nameof(type), ScanOperator.Equal, type));

            try
            {
                var batch = dynamoDbContext.ScanAsync<PersistedGrantDynamoDB>(conditions);
                while (!batch.IsDone)
                {
                    var dataset = await batch.GetNextSetAsync();

                    if (dataset.Any())
                    {
                        dataset.ForEach(async item => await dynamoDbContext.DeleteAsync(item));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default(EventId), ex, "PersistedGrantRepository.RemoveAllAsync failed with subjectId: {subjectId}, clientId: {clientId}", subjectId, clientId);
                await Task.FromException(ex);
                return;
            }

            await Task.CompletedTask;
            return;
        }

        /// <summary>
        /// Removes the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="key">Key.</param>
        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                await Task.FromException(new ArgumentNullException($"PersistedGrantRepository.RemoveAsync key:{key} is null or empty"));
                return;
            }

            try
            {
                // find object first
                var batch = await dynamoDbContext.QueryAsync<PersistedGrantDynamoDB>(key).GetRemainingAsync();

                if(batch.Any()){
                    await dynamoDbContext.DeleteAsync(batch.First());
                }
                else{
                    await Task.FromResult(0);
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default(EventId), ex, "PersistedGrantRepository.RemoveAsync failed with key {key}", key);
                await Task.FromException(ex);
                return;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Stores the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="grant">Grant.</param>
        public async Task StoreAsync(PersistedGrant grant)
        {
            try
            {
                await dynamoDbContext.SaveAsync<PersistedGrantDynamoDB>(grant.GetPersistedGrantDynamoDB());
            }
            catch (Exception ex)
            {
                logger.LogError(default(EventId), ex, "PersistedGrantRepository.StoreAsync failed with PersistedGrant {grant}", grant);
                await Task.FromException(ex);
                return;
            }

            await Task.CompletedTask;
        }
    }
}
