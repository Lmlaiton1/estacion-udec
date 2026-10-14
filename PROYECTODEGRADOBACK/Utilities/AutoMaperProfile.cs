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
            CreateMap<AlertaMeteo, AlertaMeteoDto>()
                .ForMember(d => d.VistoPorNombre, opt => opt.MapFrom(o => o.VistoPor != null ? (o.VistoPor.Name + " " + o.VistoPor.Last_Name) : null));
            CreateMap<AuditoriaMeteo, AuditoriaMeteoDto>()
                .ForMember(d => d.UsuarioNombre, opt => opt.MapFrom(o => o.Usuario != null ? (o.Usuario.Name + " " + o.Usuario.Last_Name) : string.Empty));

            // Control de cultivo
            CreateMap<EtapaCultivo, EtapaCultivoDto>();
            CreateMap<Cultivo, CultivoDto>()
                .ForMember(d => d.Etapas, opt => opt.MapFrom(o => o.Etapas != null ? o.Etapas.OrderBy(e => e.Orden).ToList() : new List<EtapaCultivo>()))
                .ForMember(d => d.EtapaActual, opt => opt.MapFrom(o => o.Etapas != null ? o.Etapas.Where(e => e.Activa).Select(e => e.Nombre).FirstOrDefault() : null))
                .ForMember(d => d.Estaciones, opt => opt.MapFrom(o => o.Estaciones != null ? o.Estaciones.Select(e => e.EstacionNumero).OrderBy(n => n).ToList() : new List<int>()));
            CreateMap<SensorCultivo, SensorCultivoDto>()
                .ForMember(d => d.CultivoNombre, opt => opt.MapFrom(o => o.Cultivo != null ? o.Cultivo.Nombre : string.Empty))
                .ForMember(d => d.UltimoValor, opt => opt.Ignore())
                .ForMember(d => d.FechaUltimoValor, opt => opt.Ignore());
            CreateMap<ActuadorCultivo, ActuadorCultivoDto>()
                .ForMember(d => d.CultivoNombre, opt => opt.MapFrom(o => o.Cultivo != null ? o.Cultivo.Nombre : string.Empty));
            CreateMap<UmbralCultivo, UmbralCultivoDto>()
                .ForMember(d => d.CultivoNombre, opt => opt.MapFrom(o => o.Cultivo != null ? o.Cultivo.Nombre : string.Empty))
                .ForMember(d => d.SensorNombre, opt => opt.MapFrom(o => o.Sensor != null ? o.Sensor.Nombre : string.Empty))
                .ForMember(d => d.Variable, opt => opt.MapFrom(o => o.Sensor != null ? o.Sensor.Variable : string.Empty))
                .ForMember(d => d.Unidad, opt => opt.MapFrom(o => o.Sensor != null ? o.Sensor.Unidad : string.Empty))
                .ForMember(d => d.EtapaNombre, opt => opt.MapFrom(o => o.Etapa != null ? o.Etapa.Nombre : null))
                .ForMember(d => d.ActuadorNombre, opt => opt.MapFrom(o => o.Actuador != null ? o.Actuador.Nombre : null));
            CreateMap<LecturaCultivo, LecturaCultivoDto>();
            CreateMap<BitacoraAlarmaCultivo, BitacoraAlarmaCultivoDto>()
                .ForMember(d => d.UsuarioNombre, opt => opt.MapFrom(o => o.Usuario != null ? (o.Usuario.Name + " " + o.Usuario.Last_Name) : "Sistema"));
            CreateMap<AlarmaCultivo, AlarmaCultivoDto>()
                .ForMember(d => d.CultivoNombre, opt => opt.MapFrom(o => o.Cultivo != null ? o.Cultivo.Nombre : string.Empty))
                .ForMember(d => d.VistaPorNombre, opt => opt.MapFrom(o => o.VistaPor != null ? (o.VistaPor.Name + " " + o.VistaPor.Last_Name) : null))
                .ForMember(d => d.AtendidaPorNombre, opt => opt.MapFrom(o => o.AtendidaPor != null ? (o.AtendidaPor.Name + " " + o.AtendidaPor.Last_Name) : null))
                .ForMember(d => d.ResueltaPorNombre, opt => opt.MapFrom(o => o.ResueltaPor != null ? (o.ResueltaPor.Name + " " + o.ResueltaPor.Last_Name) : null))
                .ForMember(d => d.Bitacora, opt => opt.MapFrom(o => o.Bitacora != null ? o.Bitacora.OrderBy(b => b.Fecha).ToList() : new List<BitacoraAlarmaCultivo>()));
            CreateMap<EventoDispositivoCultivo, EventoDispositivoCultivoDto>()
                .ForMember(d => d.UsuarioNombre, opt => opt.MapFrom(o => o.Usuario != null ? (o.Usuario.Name + " " + o.Usuario.Last_Name) : "Sistema"));
            CreateMap<AuditoriaCultivo, AuditoriaCultivoDto>()
                .ForMember(d => d.UsuarioNombre, opt => opt.MapFrom(o => o.Usuario != null ? (o.Usuario.Name + " " + o.Usuario.Last_Name) : string.Empty));

        }
    }
}