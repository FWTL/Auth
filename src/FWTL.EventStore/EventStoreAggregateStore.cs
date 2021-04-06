﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;
using FWTL.Common.Exceptions;
using FWTL.Core.Aggregates;
using FWTL.Core.Events;
using FWTL.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Polly;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;
using StreamPosition = EventStore.Client.StreamPosition;

namespace FWTL.EventStore
{
    public class EventStoreAggregateStore : IAggregateStore
    {
        private readonly IDatabase _cache;

        private readonly IServiceProvider _context;

        private readonly EventStoreClient _eventStoreClient;

        private readonly IPublishEndpoint _publishEndpoint;

        public EventStoreAggregateStore(
            EventStoreClient eventStoreClient,
            IDatabase cache,
            IServiceProvider context,
            IPublishEndpoint publishEndpoint)
        {
            _eventStoreClient = eventStoreClient;
            _cache = cache;
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<TAggregate> GetByIdAsync<TAggregate>(Guid aggregateId) where TAggregate : class, IAggregateRoot, new()
        {
            var aggregate = await GetByIdOrDefaultAsync<TAggregate>(aggregateId.ToString(), int.MaxValue);
            if (aggregate is null)
            {
                throw new AppValidationException($"{typeof(TAggregate).Name}Id", $"Aggregate with id : {aggregateId} not found");
            }

            return aggregate;
        }

        public TAggregate GetNew<TAggregate>() where TAggregate : class, IAggregateRoot, new()
        {
            var model = new TAggregate { Context = _context, Version = -1 };
            return model;
        }

        public async Task SaveAsync<TAggregate>(TAggregate aggregate) where TAggregate : class, IAggregateRoot
        {
            string streamName = $"{aggregate.GetType().Name}:{aggregate.Id}";
            IEnumerable<EventComposite> newEvents = aggregate.Events;
            List<EventData> eventsToSave = newEvents.Select(ToEventData).ToList();

            var service = _context.GetService<IAggregateMap<TAggregate>>();

            if (service != null)
            {
                if (aggregate.Version == -1)
                {
                    await Policies.SqRetryPolicy.ExecuteAsync(() => service.CreateAsync(aggregate));
                }
                else
                {
                    await Policies.SqRetryPolicy.ExecuteAsync(() => service.UpdateAsync(aggregate));
                }
            }

            var result = await Policies.EventStoreRetryPolicy.ExecuteAndCaptureAsync(() => _eventStoreClient.AppendToStreamAsync(streamName, StreamState.Any, eventsToSave));
            if (result.Outcome == OutcomeType.Successful)
            {
                aggregate.Version += aggregate.Events.Count() - 1;
                await Policies.RedisFallbackPolicy.ExecuteAsync(() => _cache.StringSetAsync(streamName, JsonSerializer.Serialize(aggregate), TimeSpan.FromDays(1)));
                return;
            }

            if (service != null)
            {
                await _publishEndpoint.Publish(new AggregateInOutOfSyncState()
                {
                    AggregateId = aggregate.Id,
                    Type = aggregate.GetType().FullName,
                    Version = aggregate.Version
                });
            }
        }

        public async Task DeleteAsync<TAggregate>(TAggregate aggregate) where TAggregate : class, IAggregateRoot
        {
            var streamName = $"{aggregate.GetType().Name}:{aggregate.Id}";
            await _cache.KeyDeleteAsync(streamName);

            var service = _context.GetService<IAggregateMap<TAggregate>>();
            if (service != null)
            {
                await service.DeleteAsync(aggregate);
            }

            //await _eventStoreClient.SoftDeleteAsync(streamName, StreamRevision.None);
        }


        private dynamic DeserializeEvent(ReadOnlyMemory<byte> metadata, ReadOnlyMemory<byte> data)
        {
            JToken eventType = JObject.Parse(Encoding.UTF8.GetString(metadata.Span)).Property("EventType")?.Value;
            if (eventType == null)
            {
                throw new NullReferenceException("EventType is null");
            }

            string json = Encoding.UTF8.GetString(data.Span);
            return JsonSerializer.Deserialize(json, Type.GetType((string)eventType));
        }

        public async Task<bool> ExistsAsync<TAggregate>(string aggregateId) where TAggregate : class, IAggregateRoot
        {
            string streamName = $"{typeof(TAggregate).Name}:{aggregateId}";
            EventStoreClient.ReadStreamResult stream = _eventStoreClient.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.FromInt64(0));

            return await stream.ReadState != ReadState.StreamNotFound;
        }

        public Task<TAggregate> GetByIdOrDefaultAsync<TAggregate>(Guid aggregateId)
            where TAggregate : class, IAggregateRoot, new()
        {
            return GetByIdOrDefaultAsync<TAggregate>(aggregateId.ToString(), int.MaxValue);
        }

        private async Task<TAggregate> GetByIdOrDefaultAsync<TAggregate>(string aggregateId, int version)
            where TAggregate : class, IAggregateRoot, new()
        {
            if (version <= 0)
            {
                throw new InvalidOperationException("Cannot get version <= 0");
            }

            var streamName = $"{typeof(TAggregate).Name}:{aggregateId}";
            TAggregate aggregate = new TAggregate
            {
                Version = -1
            };

            RedisValue value = await _cache.StringGetAsync(streamName);
            if (value.HasValue)
            {
                aggregate = JsonSerializer.Deserialize<TAggregate>(value);
            }

            long sliceStart = aggregate.Version + 1;

            EventStoreClient.ReadStreamResult stream = _eventStoreClient.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.FromInt64(sliceStart));

            if (await stream.ReadState == ReadState.StreamNotFound)
            {
                return null;
            }

            await foreach (var @event in stream)
            {
                (aggregate as dynamic).Apply(DeserializeEvent(@event.Event.Metadata, @event.Event.Data));
                aggregate.Version++;
            }

            aggregate.Context = _context;
            return aggregate;
        }

        private EventData ToEventData(EventComposite @event)
        {
            var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event.Event, @event.Event.GetType()));
            var metadata = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event.Metadata));

            return new EventData(@event.Metadata.EventId, @event.Metadata.EventType, data, metadata);
        }
    }
}