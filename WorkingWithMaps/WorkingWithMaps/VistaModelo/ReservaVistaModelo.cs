﻿using MvvmHelpers;
using PlacesApp.Views;
using Prism.Commands;
using PropertyApp.Modelo;
using PropertyApp.Servicio;
using PropertyApp.url;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using WorkingWithMaps;
using WorkingWithMaps.Modelo;
using WorkingWithMaps.Servicio;
using WorkingWithMaps.Vistas.Reservas;
using WSGOPLAY.Models;
using Xamarin.Forms;

namespace PropertyApp.VistaModelo
{
    public class ReservaVistaModelo : BaseViewModel
    {
        GoPlayServicio goplayServicio;
        private string nombre;
        public string Nombre
        {
            get
            {
                return nombre;
            }
            set
            {
                SetProperty(ref nombre, value);
                SetProperty(ref concepto, value);
            }
        }

        private string hora;
        public string Hora
        {
            get { return hora; }
            set
            {
                SetProperty(ref hora, value);
                SetProperty(ref concepto, value);
            }
        }
        private string precio;
        public string Precio
        {
            get { return Convert.ToInt32(precio).ToString("#,##0"); }
            set
            {
                SetProperty(ref precio, value);
                SetProperty(ref concepto, value);
            }
        }

        private string fecha = DateTime.Now.Date.ToShortDateString();
        public string Fecha
        {
            get => fecha;
            set
            {
                SetProperty(ref fecha, value);
                SetProperty(ref concepto, value);
            }
        }

        public DateTime FechaMin
        {
            get => DateTime.Now.Date;
        }

        public ICommand FechaComando => new Command(async () =>
        {
            //await Application.Current.MainPage.DisplayAlert("", Fecha, "OK");
            IsBusy = true;
            Load();
            IsBusy = false;
        });

        public DelegateCommand ResumenComando
        {
            get
            {
                return new DelegateCommand(() =>
                {

                    Application.Current.MainPage.Navigation.PushAsync(new PgReserva()
                    {
                        BindingContext = this
                    }
                   );
                });
            }
        }

        public string codigoVerificacion { get; set; }

        public DelegateCommand ContinuarComando
        {
            get
            {

                return new DelegateCommand(async () =>
                {
                   
                    if (string.IsNullOrEmpty(hora))
                    {
                        await Application.Current.MainPage.DisplayAlert("", "Por favor seleccione una hora", "OK");
                        IsBusy = false;
                        return;
                    }

                    if (string.IsNullOrEmpty(fecha))
                    {
                        await Application.Current.MainPage.DisplayAlert("", "Por favor seleccione una fecha", "OK");
                        IsBusy = false;
                        return;
                    }

                    // VERIFICACION DEL NUMERO DE TELEFONO:::
                    try
                    {
                        var tel = await Application.Current.MainPage.DisplayPromptAsync("GOPLAY:", "Numero de telefono", "OK", "Cancel", null, 10, keyboard: Keyboard.Numeric, "");
                        if (tel.Length == 10)
                        {
                            IsBusy = true;
                            var reserva = new ReservaModelo();
                            AmazonWSnsn amazonW = new AmazonWSnsn();
                            var codigo = await amazonW.VerificarTel(tel);

                            Concepto = $"Reserva de cancha {nombre} para la fecha {fecha} hora {hora} por el valor de {precio}";
                            reserva.Idhorario = horariosSelect.Id;
                            reserva.Idestado = 6;
                            reserva.Fecha = horariosSelect.Fecha + " " + DateTime.Now.ToShortTimeString();
                            reserva.HoraInicio = horariosSelect.Hora;
                            reserva.HoraFinal = horariosSelect.Precio.ToString();
                            reserva.Idhorario = horariosSelect.Id;
                            reserva.Reto = "";
                            reserva.Tel = tel;
                            reserva.codigoVerificacion = codigo;
                            reserva.Usuario = tel;
                            reserva.Reserva1 = concepto;
                            codigoVerificacion = codigo;

                            var es = await ReservaEstadoStatic.Estado(reserva.Idhorario, reserva.Fecha, reserva.HoraInicio);
                            if (es == false)
                            {
                                //var goplay = new GoPlayServicio();
                                //var res = await goplay.PostGuardarAsync(reserva, Url.urlReserva);
                                var res = await OperacionesCRUD.GuardarAsync(reserva, Url.urlReserva);
                                if (res == null)
                                {
                                    //reserva.IsBusy = false;
                                    IsBusy = false;
                                    await  Application.Current.MainPage.DisplayAlert("", "Lo sentomos al parecer la cancha no esta disponible \n vuelva a intentarlo", "OK");
                                    return;
                                }
                                reserva.IdReserva = res.IdReserva;
                            }

                            var respuesta = await Application.Current.MainPage.DisplayAlert("", "Debe completar la reserva en 15 minutos \n pasado el tiempo la cancha estara disponible", "OK", "CANCELAR");
                            
                                await Application.Current.MainPage.Navigation.PushModalAsync(
                            new ConfirmarTelVista { BindingContext = reserva });
                            
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Error", "Numero de no valido", "OK");
                            return;
                        }
                        IsBusy = false;
                    }
                    catch (Exception)
                    {
                        IsBusy = false;
                        //throw;
                    }
                   
                });
            }
        }


        //MIS RESERVAS

        private ObservableCollection<ReservaModelo> misReservas;

        public ObservableCollection<ReservaModelo> MisReservas
        {
            get => misReservas;
            set => SetProperty(ref misReservas, value);
        }

        private string telefono;

        public string Telefono
        {
            get => telefono;
            set => SetProperty(ref telefono, value);
        }


        public ICommand buscarReserca => new Command<string>(async (string usuario) =>
        {
            IsBusy = true;
            var reservas = await goplayServicio.GetDatosAsync<Reserva>(Url.urlMisReservaEstado + usuario);
            if (reservas.Count > 0)
            {
                var reservasModelo = new ObservableCollection<ReservaModelo>();
                foreach (var item in reservas)
                {
                    reservasModelo.Add(new ReservaModelo
                    {
                        IdReserva = item.IdReserva,
                        Fecha = item.Fecha,
                        HoraFinal = item.HoraFinal,
                        HoraInicio = item.HoraInicio,
                        Idestado = item.Idestado,
                        Idhorario = item.Idhorario,
                        Reserva1 = item.Reserva1,
                        Reto = item.Reto,
                        Usuario = item.Usuario,
                        IdestadoNavigation = item.IdestadoNavigation,
                        IdhorarioNavigation = item.IdhorarioNavigation
                    });
                }
                MisReservas = reservasModelo;
            }
            IsBusy = false;
            //await Task.Delay(20);
        });



        public string Id { get; set; }
        public string Image { get; set; }
        public string SubTitle { get; set; }
        public string Description { get; set; }
        public string Foto { get; set; }
        public string Estado { get; set; }

        private string celular;
        public string Celular
        {
            get => $"{"Celular: "} {celular}";
            set => SetProperty(ref celular, value);
        }
        public string User { get; set; }
        public string Creada { get; set; }
        //public string Direccion { get; set; }
        private string direccion;
        public string Direccion
        {
            get => $"{"Dirección:"} {direccion}";
            set => SetProperty(ref direccion, value);
        }
        public List<string> Images { get; set; }

        private string concepto = $"{"Reserva de cancha"}  {"para la fecha "}";
        public string Concepto
        {
            get { return $"Reserva de cancha {nombre} para la fecha {fecha} hora {hora} por el valor de {precio}"; }
            set { SetProperty(ref concepto, value); }
        }
        private ObservableCollection<Horario> horario;
        public ObservableCollection<Horario> Horario
        {
            get => horario;
            set => SetProperty(ref horario, value);
        }

        private string idCancha;

        public string IdCancha
        {
            get => idCancha;
            set
            {
                SetProperty(ref idCancha, value);

            }
        }

        //public string idCancha { get; set; }

        private WoPages paginaHorario;

        public WoPages PaginaHorario
        {
            get => paginaHorario;
            set => SetProperty(ref paginaHorario, value);
        }

        private ObservableCollection<HorarioModelo> horarios;

        public ObservableCollection<HorarioModelo> Horarios
        {
            get => horarios;
            set => SetProperty(ref horarios, value);
        }

        private HorarioModelo horariosSelect;

        public HorarioModelo HorariosSelect
        {
            get => horariosSelect;
            set
            {
                SetProperty(ref horariosSelect, value);
                SetProperty(ref nombre, HorariosSelect.NombreCancha);
                SetProperty(ref hora, HorariosSelect.Hora);
                SetProperty(ref precio, HorariosSelect.Precio.ToString("#,##0"));
            }
        }

        public ReservaVistaModelo()
        {

            goplayServicio = new GoPlayServicio();
            //IdCancha = id;
            //Load();
            //Task.Run( async () => await Load()) ;
        }

        public async Task Load()
        {
            try
            {
                IsBusy = true;
                //var PaginaHorario = await goplayServicio.Get<WoPages>(Url.urlPagesid + idCancha);

                var horario = new ObservableCollection<HorarioModelo>();
                var fe = Convert.ToDateTime(this.fecha).ToString().Substring(0, 10).Trim();
                var PaginaHorario = await goplayServicio.GetDatoAsync<WoPages>(Url.urlPageHorario + idCancha);
                var reservaCancha = await goplayServicio.GetDatosAsync<Reserva>(Url.urlReservaCancha + idCancha + "/" + fe.Replace("/", ""));

                if (PaginaHorario == null)
                {
                    await Application.Current.MainPage.DisplayAlert("", "Sin resultados", "OK");
                    return;
                }

                foreach (var item in PaginaHorario.Horario)
                {
                    var variables = new HorarioModelo();
                    variables.Id = item.Id;
                    variables.NombreCancha = PaginaHorario.PageTitle;
                    variables.Imagen = PaginaHorario.Avatar;
                    variables.Hora = item.ProNombre;
                    if (string.IsNullOrEmpty(item.ProPrecio.ToString()))
                        variables.Precio = Convert.ToDecimal(0);
                    else
                        variables.Precio = Convert.ToDecimal(item.ProPrecio);

                    if (reservaCancha.Count > 0)
                    {
                        foreach (var res in reservaCancha)
                        {
                            var fecha = (res.Fecha.Length > 10) ? res.Fecha.Substring(0, 9).Trim() : res.Fecha;
                            if (fecha == fe && item.ProNombre == res.HoraInicio)
                            {
                                if (res.IdestadoNavigation.Descripcion == "Ocupada")
                                {
                                    variables.Estado = res.IdestadoNavigation.Descripcion;
                                    variables.Color = "Red";
                                    variables.idEstado = res.IdestadoNavigation.Id;

                                }
                                if (res.IdestadoNavigation.Descripcion == "Pendiente")
                                {
                                    variables.Estado = res.IdestadoNavigation.Descripcion;
                                    variables.Color = "Orange";
                                    variables.idEstado = res.IdestadoNavigation.Id;
                                }

                                if (res.IdestadoNavigation.Descripcion == "En reserva")
                                {
                                    variables.Estado = res.IdestadoNavigation.Descripcion;
                                    variables.Color = "Yellow";
                                    variables.idEstado = res.IdestadoNavigation.Id;
                                }

                                if (res.IdestadoNavigation.Descripcion == "Pagada")
                                {
                                    variables.Estado = res.IdestadoNavigation.Descripcion;
                                    variables.Color = "Blue";
                                    variables.idEstado = res.IdestadoNavigation.Id;
                                }
                                break;
                            }
                            else
                            {
                                variables.Estado = "Disponible";
                                variables.idEstado = 3;
                                variables.Color = "Green";
                            }

                        }
                    }
                    else
                    {
                        variables.Estado = "Disponible";
                        variables.idEstado = 3;
                        variables.Color = "Green";
                    }


                    if (item.Sinreserva.Count > 0)
                    {
                        foreach (var sinreserva in item.Sinreserva)
                        {
                            if (sinreserva.Sinidhorario == item.Id && fe == sinreserva.Sinfecha)
                            {
                                variables.Estado = "Cerrado";
                                variables.Color = "Red";
                                variables.idEstado = 4;

                            }
                        }
                    }

                    variables.Fecha = this.Fecha;

                    horario.Add(variables);
                }
                Horarios = horario;
                IsBusy = false;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("", ex.ToString(), "OK");
                throw;
            }
            //await Application.Current.MainPage.DisplayAlert("", Horarios.Count.ToString(), "OK");
        }

    }
}
