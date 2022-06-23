﻿using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Tinkerforge;

namespace Bonsai.Tinkerforge
{
    [DefaultProperty(nameof(Uid))]
    [Description("Measures CO2 concentration, in ppm, temperature, and humidity from a CO2 Bricklet 2.0.")]
    public class CO2V2 : Combinator<IPConnection, CO2V2.Co2DataFrame>
    {
        [TypeConverter(typeof(UidConverter))]
        [Description("Device data including address UID.")]
        public string Uid { get; set; }

        [Description("Specifies the period between sample event callbacks. A value of zero disables event reporting.")]
        public long Period { get; set; } = 1000;

        [Description("Specifies the ambient air pressure. Can be used to increase the accuracy of the CO2 sensor.")]
        public int AirPressure { get; set; }

        [Description("Specifies a temperature offset, in hundredths of a degree, to compensate for heat inside an enclosure.")]
        public int TemperatureOffset { get; set; }

        [Description("Specifies the behavior of the status LED.")]
        public BrickletCO2V2StatusLedConfig StatusLed { get; set; } = BrickletCO2V2StatusLedConfig.ShowStatus;

        public override string ToString()
        {
            return BrickletCO2V2.DEVICE_DISPLAY_NAME;
        }

        public override IObservable<Co2DataFrame> Process(IObservable<IPConnection> source)
        {
            return source.SelectStream(connection =>
            {
                var device = new BrickletCO2V2(Uid, connection);
                connection.Connected += (sender, e) =>
                {
                    device.SetStatusLEDConfig((byte)StatusLed);
                    device.SetAirPressure(AirPressure);
                    device.SetTemperatureOffset(TemperatureOffset);
                    device.SetAllValuesCallbackConfiguration(Period, false);
                };

                return Observable.Create<Co2DataFrame>(observer =>
                {
                    BrickletCO2V2.AllValuesEventHandler handler = (sender, co2Concentration, temperature, humidity) =>
                    {
                        observer.OnNext(new Co2DataFrame(co2Concentration, temperature, humidity));
                    };

                    device.AllValuesCallback += handler;
                    return Disposable.Create(() =>
                    {
                        try { device.SetAllValuesCallbackConfiguration(0, false); }
                        catch (NotConnectedException) { } // best effort
                        device.AllValuesCallback -= handler;
                    });
                });
            });
        }

        public struct Co2DataFrame
        {
            public int Co2Concentration;
            public short Temperature;
            public int Humidity;

            public Co2DataFrame(int co2Concentration, short temperature, int humidity)
            {
                Co2Concentration = co2Concentration;
                Temperature = temperature;
                Humidity = humidity;
            }
        }

        public enum BrickletCO2V2StatusLedConfig : byte
        {
            Off = BrickletCO2V2.STATUS_LED_CONFIG_OFF,
            On = BrickletCO2V2.STATUS_LED_CONFIG_ON,
            ShowHeartbeat = BrickletCO2V2.STATUS_LED_CONFIG_SHOW_HEARTBEAT,
            ShowStatus = BrickletCO2V2.STATUS_LED_CONFIG_SHOW_STATUS
        }
    }
}