﻿using System;
using Automatonymous;
using FWTL.Events;

namespace FWTL.Domain.Accounts.AccountSetup
{
    //https://miro.com/app/board/o9J_lDZlCU8=/
    public class AccountSetupSaga : MassTransitStateMachine<AccountSetupState>
    {
        public State Initialized { get; }

        public State Setup { get; }

        public AccountSetupSaga()
        {
            Event(() => AccountCreated, x =>
            {
                x.CorrelateById(m => m.Message.AccountId);
                x.SetSagaFactory(context => new AccountSetupState
                {
                    CorrelationId = context.Message.AccountId,
                    ExpirationTokenId = Guid.NewGuid(),
                });
            });

            Event(() => SessionCreated, x => x.CorrelateById(m => m.Message.AccountId));
            Event(() => CodeSent, x => x.CorrelateById(m => m.Message.AccountId));
            Event(() => AccountVerified, x => x.CorrelateById(m => m.Message.AccountId));

            Event(() => SetupFailed, x => x.CorrelateById(m => m.Message.AccountId));
            
            Event(() => AccountSetupRestarted, x => x.CorrelateById(m => m.Message.AccountId));
            Event(() => AccountDeleted, x => x.CorrelateById(m => m.Message.AccountId));

            InstanceState(x => x.CurrentState, Initialized, Setup);

            Schedule(() => Timeout, instance => instance.ExpirationTokenId, s =>
            {
                s.Delay = TimeSpan.FromMinutes(10);
            });

            Initially(When(AccountCreated)
                .TransitionTo(Initialized)
                .Publish(x => new CreateSession.Command() { CorrelationId = x.Data.CorrelationId, AccountId = x.Instance.CorrelationId }));

            During(Initialized, When(SessionCreated)
                .TransitionTo(Setup)
                .Publish(x => new SendCode.Command() { CorrelationId = x.Data.CorrelationId, AccountId = x.Instance.CorrelationId }));

            During(Setup, When(CodeSent)
                .Schedule(Timeout, context => new RemoveSession.Command() { CorrelationId = context.Data.CorrelationId, AccountId = context.Instance.CorrelationId }));

            During(Setup, When(AccountVerified)
                .Unschedule(Timeout).Finalize());

            During(Setup, When(SetupFailed)
                .Unschedule(Timeout)
                .Publish(x => new RemoveSession.Command() { CorrelationId = x.Data.CorrelationId, AccountId = x.Instance.CorrelationId })
                .Finalize());

            During(Initialized, When(SetupFailed).Finalize());

            DuringAny(When(AccountSetupRestarted).Finalize());
            DuringAny(When(AccountDeleted).Finalize());

            DuringAny(When(Timeout.Received).Publish(x => x.Data));

            SetCompletedWhenFinalized();
        }

        public Event<AccountCreated> AccountCreated { get; }

        public Event<SessionCreated> SessionCreated { get; }

        public Event<CodeSent> CodeSent { get; }

        public Event<AccountVeryfied> AccountVerified { get; }

        public Event<SetupFailed> SetupFailed { get; }

        public Event<AccountSetupRestarted> AccountSetupRestarted { get; }

        public Event<AccountDeleted> AccountDeleted { get; }

        public Schedule<AccountSetupState, RemoveSession.Command> Timeout { get; }
    }
}