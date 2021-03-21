﻿using System.Threading.Tasks;
using FWTL.Core.Services.Dto;
using FWTL.TelegramClient;

namespace FWTL.Core.Services.Telegram
{
    public interface IContactService
    {
        Task<ResponseWrapper<ContactsContacts>> GetAllContactsAsync(string sessionName);

        Task<ResponseWrapper<Info>> GetInfoAsync(string sessionName, Dialog.DialogType type, int id);

        Task<ResponseWrapper<Info>> GetInfoAsync(string sessionName, string dialogId);
    }
}