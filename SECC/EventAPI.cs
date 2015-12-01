
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel.Web;
using Arena.Security;
using Arena.Custom.HDC.WebService.Contracts;
using Arena.Services.Contracts;
using Arena.Event;
using Arena.Services.Exceptions;

namespace Arena.Custom.HDC.WebService.SECC
{
    class EventAPI
    {
        /// <summary>
        /// <b>GET event/{id}</b>
        ///
        /// Retrieve an event from Arena
        /// </summary>
        /// <returns>Event object.</returns>
        [WebGet(UriTemplate = "event/{id}")]
        public Contracts.Event GetEvent(int id, string fields)
        {
            Arena.Event.EventProfile arenaEvent = new Arena.Event.EventProfile(id);
            if (arenaEvent == null)
            {
                throw new ResourceNotFoundException("Invalid event id");
            }
            Contracts.EventMapper eventMapper = new Contracts.EventMapper();

            return eventMapper.FromArena(arenaEvent);
        }

    }
}
