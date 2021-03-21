﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FWTL.Core.Services.Dto;
using FWTL.Core.Services.Telegram;

namespace FWTL.TelegramClient.Services
{
    public class ContactService : BaseService, IContactService
    {
        public ContactService(HttpClient client) : base(client)
        {
        }

        public Task<ResponseWrapper<ContactsContacts>> GetAllContactsAsync(string sessionName)
        {
            throw new NotImplementedException();
        }

        public Task<ResponseWrapper<Info>> GetInfoAsync(string sessionName, Dialog.DialogType type, int id)
        {
            throw new NotImplementedException();
        }

        public Task<ResponseWrapper<Info>> GetInfoAsync(string sessionName, string dialogId)
        {
            throw new NotImplementedException();
        }
    }
}