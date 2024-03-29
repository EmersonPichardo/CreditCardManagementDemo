﻿using Application._Common;
using Application._Common.Security.Authentication;
using Domain._Common.Entities.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure._Common.Persistence.Databases.Interceptors;

internal class AuditableEntitySaveChangesInterceptor(
    IIdentityService identityService,
    IClockService clockService
)
    : SaveChangesInterceptor
{
    private readonly Guid? currenUserId = identityService
        .GetCurrentUserIdentity()?
        .Id;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditableData(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditableData(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableData(DbContext? context)
    {
        if (context is null)
            return;

        var entitiesTracked = context
            .ChangeTracker
            .Entries<IAuditableEntity>();

        foreach (var entry in entitiesTracked)
            UpdateEntityByState(entry);
    }
    private void UpdateEntityByState(EntityEntry<IAuditableEntity> entry)
    {
        var state = entry.State;

        if (state is EntityState.Added)
        {
            UpdateAddedData(entry);
            UpdateModifiedData(entry);
        }

        if (state is EntityState.Modified)
            UpdateModifiedData(entry);

        if (state is EntityState.Deleted)
            UpdateDeletedData(entry);
    }
    private void UpdateAddedData(EntityEntry<IAuditableEntity> entry)
    {
        var entity = entry.Entity;

        entity.CreatedBy = currenUserId;
        entity.CreationDate = clockService.Now;
    }
    private void UpdateModifiedData(EntityEntry<IAuditableEntity> entry)
    {
        var entity = entry.Entity;

        entity.LastModifiedBy = currenUserId;
        entity.LastModificationDate = clockService.Now;
    }
    private void UpdateDeletedData(EntityEntry<IAuditableEntity> entry)
    {
        var entity = entry.Entity;

        entity.IsDeleted = true;
        entity.DeletedBy = currenUserId;
        entity.DeletionDate = clockService.Now;

        entry.State = EntityState.Modified;
    }
}