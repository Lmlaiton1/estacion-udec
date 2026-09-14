using AutoMapper;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using RR_Nueva_Naturaleza.Models;
using RR_Nueva_Naturaleza.Models.Dto;
using RR_Nueva_Naturaleza.Models.Request;
using RR_Nueva_Naturaleza.Models.Response;
using System.Runtime.InteropServices;

namespace RR_Nueva_Naturaleza.Utilities
{
    public class AutoMaperProfile : Profile
    {
        public AutoMaperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<Event, EventNotifyDto>();
            CreateMap<User, UserRequest>().ForMember(destiny => destiny.RolName, opt => opt.MapFrom(origen => origen.Rol.Name));
            CreateMap<Device, DeviceRequest>()
                .ForMember(destiny => destiny.SystemName, opt => opt.MapFrom(origen => origen.System_Type.System_Type_Name))
                .ForMember(destiny => destiny.Device_StatusName, opt => opt.MapFrom(origen => origen.Device_Status.State_Name));
            CreateMap<Rol, RolDto>();
            CreateMap<Device, DeviceDto>().ForMember(d => d.Device_StatusName, opt => opt.MapFrom(o => o.Device_Status.State_Name));
            CreateMap<System_Type, System_TypeDto>();
            CreateMap<Device_Status, Device_StatusDto>();
            CreateMap<Unit_Measurement, Unit_MeasurementDto>();

            // Dominio Estación Meteorológica
            CreateMap<LecturaMeteo, LecturaMeteoDto>();
            CreateMap<UmbralMeteo, UmbralMeteoDto>();
            CreateMap<AlertaMeteo, AlertaMeteoDto>();

        }
    }
}