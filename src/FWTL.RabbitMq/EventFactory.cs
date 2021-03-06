﻿using System.Collections.Generic;
using EventStore.Client;
using FWTL.Core.Events;
using MassTransit;

namespace FWTL.RabbitMq
{
    public class EventFactory : IEventFactory
    {
        private readonly ConsumeContext _context;

        public EventFactory(ConsumeContext context)
        {
            _context = context;
        }

        public IEnumerable<EventComposite> Make(IEnumerable<EventComposite> @events)
        {
            foreach (var @event in @events)
            {
                @event.Metadata.EventId = Uuid.NewUuid();
                @event.Event.CorrelationId = _context.CorrelationId.Value;
                @event.Metadata.EventType = @event.Event.GetType().AssemblyQualifiedName;
            }

            return @events;
        }
    }
}