﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bit.Api.Controllers;
using Bit.Api.Models.Request.Organizations;
using Bit.Api.Models.Response.Organizations;
using Bit.Api.Test.AutoFixture.Attributes;
using Bit.Core.Context;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Business;
using Bit.Core.Models.OrganizationConnectionConfigs;
using Bit.Core.OrganizationFeatures.OrganizationConnections.Interfaces;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using Bit.Test.Common.Helpers;
using NSubstitute;
using Xunit;

namespace Bit.Api.Test.Controllers
{
    [ControllerCustomize(typeof(OrganizationConnectionsController))]
    [SutProviderCustomize]
    [JsonDocumentCustomize]
    public class OrganizationConnectionsControllerTests
    {
        public static IEnumerable<object[]> ConnectionTypes =>
            Enum.GetValues<OrganizationConnectionType>().Select(p => new object[] { p });


        [Theory]
        [BitAutoData(true, true)]
        [BitAutoData(false, true)]
        [BitAutoData(true, false)]
        [BitAutoData(false, false)]
        public void ConnectionEnabled_RequiresBothSelfHostAndCommunications(bool selfHosted, bool enableCloudCommunication, SutProvider<OrganizationConnectionsController> sutProvider)
        {
            var globalSettingsMock = sutProvider.GetDependency<IGlobalSettings>();
            globalSettingsMock.SelfHosted.Returns(selfHosted);
            globalSettingsMock.EnableCloudCommunication.Returns(enableCloudCommunication);

            Action<bool> assert = selfHosted && enableCloudCommunication ? Assert.True : Assert.False;

            var result = sutProvider.Sut.ConnectionsEnabled();

            assert(result);
        }

        [Theory]
        [BitAutoData]
        public async Task CreateConnection_RequiresOwnerPermissions(SutProvider<OrganizationConnectionsController> sutProvider)
        {
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => sutProvider.Sut.CreateConnection(null));

            Assert.Contains("Only the owner of an organization can create a connection.", exception.Message);
        }

        [Theory]
        [BitMemberAutoData(nameof(ConnectionTypes))]
        public async Task CreateConnection_OnlyOneConnectionOfEachType(OrganizationConnectionType type,
            OrganizationConnectionRequestModel model, BillingSyncConfig config, Guid existingEntityId,
            SutProvider<OrganizationConnectionsController> sutProvider)
        {
            model.Type = type;
            model.Config = JsonDocumentFromObject(config);
            var typedModel = new OrganizationConnectionRequestModel<BillingSyncConfig>(model);
            var existing = typedModel.ToData(existingEntityId).ToEntity();

            sutProvider.GetDependency<ICurrentContext>().OrganizationOwner(model.OrganizationId).Returns(true);

            sutProvider.GetDependency<IOrganizationConnectionRepository>().GetByOrganizationIdTypeAsync(model.OrganizationId, type).Returns(new[] { existing });

            var exception = await Assert.ThrowsAsync<BadRequestException>(() => sutProvider.Sut.CreateConnection(model));

            Assert.Contains($"The requested organization already has a connection of type {model.Type}. Only one of each connection type may exist per organization.", exception.Message);
        }

        [Theory]
        [BitAutoData]
        public async Task CreateConnection_Success(OrganizationConnectionRequestModel model, BillingSyncConfig config,
            Guid cloudOrgId, SutProvider<OrganizationConnectionsController> sutProvider)
        {
            model.Config = JsonDocumentFromObject(config);
            var typedModel = new OrganizationConnectionRequestModel<BillingSyncConfig>(model);
            typedModel.ParsedConfig.CloudOrganizationId = cloudOrgId;

            sutProvider.GetDependency<ICreateOrganizationConnectionCommand>().CreateAsync<BillingSyncConfig>(default)
                .ReturnsForAnyArgs(typedModel.ToData(Guid.NewGuid()).ToEntity());
            sutProvider.GetDependency<ICurrentContext>().OrganizationOwner(model.OrganizationId).Returns(true);
            sutProvider.GetDependency<ILicensingService>()
                .ReadOrganizationLicenseAsync(Arg.Any<Guid>())
                .Returns(new OrganizationLicense
                {
                    Id = cloudOrgId,
                });

            await sutProvider.Sut.CreateConnection(model);

            await sutProvider.GetDependency<ICreateOrganizationConnectionCommand>().Received(1)
                .CreateAsync(Arg.Is(AssertHelper.AssertPropertyEqual(typedModel.ToData())));
        }

        [Theory]
        [BitAutoData]
        public async Task UpdateConnection_RequiresOwnerPermissions(SutProvider<OrganizationConnectionsController> sutProvider)
        {
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => sutProvider.Sut.UpdateConnection(default, null));

            Assert.Contains("Only the owner of an organization can update a connection.", exception.Message);
        }

        [Theory]
        [BitMemberAutoData(nameof(ConnectionTypes))]
        public async Task UpdateConnection_OnlyOneConnectionOfEachType(OrganizationConnectionType type,
            OrganizationConnection existing1, OrganizationConnection existing2, BillingSyncConfig config,
            SutProvider<OrganizationConnectionsController> sutProvider)
        {
            existing1.Config = JsonSerializer.Serialize(config);
            var typedModel = RequestModelFromEntity(existing1);

            sutProvider.GetDependency<ICurrentContext>().OrganizationOwner(typedModel.OrganizationId).Returns(true);

            sutProvider.GetDependency<IOrganizationConnectionRepository>().GetByOrganizationIdTypeAsync(typedModel.OrganizationId, type).Returns(new[] { existing1, existing2 });

            var exception = await Assert.ThrowsAsync<BadRequestException>(() => sutProvider.Sut.UpdateConnection(existing1.Id, typedModel));

            Assert.Contains($"The requested organization already has a connection of type {typedModel.Type}. Only one of each connection type may exist per organization.", exception.Message);
        }

        [Theory]
        [BitAutoData]
        public async Task UpdateConnection_Success(OrganizationConnection existing, BillingSyncConfig config,
            OrganizationConnection updated,
            SutProvider<OrganizationConnectionsController> sutProvider)
        {
            updated.Config = JsonSerializer.Serialize(config);
            updated.Id = existing.Id;
            var model = RequestModelFromEntity(updated);

            sutProvider.GetDependency<ICurrentContext>().OrganizationOwner(model.OrganizationId).Returns(true);
            sutProvider.GetDependency<IOrganizationConnectionRepository>().GetByOrganizationIdTypeAsync(model.OrganizationId, model.Type).Returns(new[] { existing });
            sutProvider.GetDependency<IUpdateOrganizationConnectionCommand>().UpdateAsync<BillingSyncConfig>(default).ReturnsForAnyArgs(updated);

            var expected = new OrganizationConnectionResponseModel(updated, typeof(BillingSyncConfig));
            var result = await sutProvider.Sut.UpdateConnection(existing.Id, model);

            AssertHelper.AssertPropertyEqual(expected, result);
            await sutProvider.GetDependency<IUpdateOrganizationConnectionCommand>().Received(1)
                .UpdateAsync(Arg.Is(AssertHelper.AssertPropertyEqual(model.ToData(updated.Id))));
        }

        [Theory]
        [BitAutoData]
        public async Task GetConnection_RequiresOwnerPermissions(Guid connectionId, SutProvider<OrganizationConnectionsController> sutProvider)
        {
            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                sutProvider.Sut.GetConnection(connectionId, OrganizationConnectionType.CloudBillingSync));

            Assert.Contains("Only the owner of an organization can retrieve a connection.", exception.Message);
        }

        [Theory]
        [BitAutoData]
        public async Task GetConnection_Success(OrganizationConnection connection, BillingSyncConfig config,
            SutProvider<OrganizationConnectionsController> sutProvider)
        {
            connection.Config = JsonSerializer.Serialize(config);

            sutProvider.GetDependency<IOrganizationConnectionRepository>().GetByOrganizationIdTypeAsync(connection.OrganizationId, connection.Type).Returns(new[] { connection });
            sutProvider.GetDependency<ICurrentContext>().OrganizationOwner(connection.OrganizationId).Returns(true);

            var expected = new OrganizationConnectionResponseModel(connection, typeof(BillingSyncConfig));
            var actual = await sutProvider.Sut.GetConnection(connection.OrganizationId, connection.Type);

            AssertHelper.AssertPropertyEqual(expected, actual);
        }

        [Theory]
        [BitAutoData]
        public async Task DeleteConnection_NotFound(Guid connectionId,
            SutProvider<OrganizationConnectionsController> sutProvider)
        {
            await Assert.ThrowsAsync<NotFoundException>(() => sutProvider.Sut.DeleteConnection(connectionId));
        }

        [Theory]
        [BitAutoData]
        public async Task DeleteConnection_RequiresOwnerPermissions(OrganizationConnection connection,
            SutProvider<OrganizationConnectionsController> sutProvider)
        {
            sutProvider.GetDependency<IOrganizationConnectionRepository>().GetByIdAsync(connection.Id).Returns(connection);

            var exception = await Assert.ThrowsAsync<BadRequestException>(() => sutProvider.Sut.DeleteConnection(connection.Id));

            Assert.Contains("Only the owner of an organization can remove a connection.", exception.Message);
        }

        [Theory]
        [BitAutoData]
        public async Task DeleteConnection_Success(OrganizationConnection connection,
            SutProvider<OrganizationConnectionsController> sutProvider)
        {
            sutProvider.GetDependency<IOrganizationConnectionRepository>().GetByIdAsync(connection.Id).Returns(connection);
            sutProvider.GetDependency<ICurrentContext>().OrganizationOwner(connection.OrganizationId).Returns(true);

            await sutProvider.Sut.DeleteConnection(connection.Id);

            await sutProvider.GetDependency<IDeleteOrganizationConnectionCommand>().DeleteAsync(connection);
        }

        private static OrganizationConnectionRequestModel<BillingSyncConfig> RequestModelFromEntity(OrganizationConnection entity)
        {
            return new(new OrganizationConnectionRequestModel()
            {
                Type = entity.Type,
                OrganizationId = entity.OrganizationId,
                Enabled = entity.Enabled,
                Config = JsonDocument.Parse(entity.Config),
            });
        }

        private static JsonDocument JsonDocumentFromObject<T>(T obj) => JsonDocument.Parse(JsonSerializer.Serialize(obj));
    }
}
