using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTA_Speed_Mod
{
    public class SpeedMod : Script
    {

        private bool _activateMod, _timeStarted, _isDriverDriving;
        private int _currentScore, _runTime;
        private readonly Vector3 _busPosition, _startPosition;
        private Vehicle _bus;
        private Ped _driver;
        private Blip _busBlip;
        public SpeedMod()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            _busPosition = new Vector3(-432.65f, -421.4f, 32.8f);
            _startPosition = new Vector3(-554.9f, -387f, 35f);
            _isDriverDriving = _activateMod = _timeStarted = false;
            _currentScore = _runTime = 0;
        }


        public void OnTick(object sender, EventArgs e)
        {
            var player = Game.Player.Character;
            //UI.ShowSubtitle("X: " + player.Position.X + " Y: " + player.Position.Y + " Z: " + player.Position.Z);
            if (!_activateMod)
            {
                if (World.GetDistance(player.Position, _startPosition) < 1f)
                    UI.ShowSubtitle("Press F to start the mission!");
                return;
            }

            if (Game.Player.WantedLevel > 0)
            {
                UI.ShowSubtitle("Lose the cops");
                return;
            }

            Vehicle currentVehicle = null;
            if (Game.Player.Character.IsInVehicle())
                currentVehicle = player.CurrentVehicle;
            if (currentVehicle == null && _activateMod)
            {
                UI.ShowSubtitle("Enter the ~b~ bus");
                var vehicles = World.GetNearbyVehicles(Game.Player.Character.Position, 5);
                foreach (
                    var v in
                        vehicles.Where(v => v.Model == "Bus" && World.GetDistance(player.Position, _busPosition) < 10f))
                {
                    player.SetIntoVehicle(v, VehicleSeat.Passenger);
                    _busBlip.Remove();
                }
            }

            if (currentVehicle == null || currentVehicle.Model != "Bus") return;

            if (!_isDriverDriving)
            {
                _driver.Task.DriveTo(currentVehicle, new Vector3(-1336f, -3044f, 13.9f), 10f, 75f, 1074528293);
                _isDriverDriving = true;
            }
            else
            {
                foreach (var v in World.GetNearbyVehicles(player.Position, 50).Where(v => v != currentVehicle))
                {
                    v.Delete();
                }

                foreach (var p in World.GetNearbyPeds(player.Position, 20).Where(p => !p.IsInVehicle()))
                    p.Delete();
            }


            var speed = currentVehicle.Speed;
            var metersPerHour = speed*3600f;
            var mph = metersPerHour/1609.344f;

            if ((int) mph > 55)
            {
                _driver.Delete();
                player.SetIntoVehicle(currentVehicle, VehicleSeat.Driver);

                _timeStarted = true;
                _isDriverDriving = false;
            }
            if (_timeStarted)
            {
                if ((int) mph < 50)
                {
                    _bus.Explode();
                    _isDriverDriving = _activateMod = _timeStarted = false;
                    _currentScore = _runTime = 0;
                    _driver = null;
                    _bus = null;
                    return;
                }
                _runTime++;
                if (_runTime%40 == 0)
                    _currentScore++;
            }

            UI.ShowSubtitle("Current Speed: " + (int) mph + ((_timeStarted) ? " Current Score: " + _currentScore : ""));
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                foreach (var v in World.GetNearbyVehicles(Game.Player.Character.Position, 10))
                    v.Delete();
            }
            if (e.KeyCode == Keys.F3)
                Game.Player.Character.Position = _busPosition;
            if (e.KeyCode == Keys.F && World.GetDistance(Game.Player.Character.Position, _startPosition) < 2f &&
                !_activateMod)
            {
                //Check for previous spawned busses
                foreach (var b in World.GetActiveBlips().Where(b => (b.Color == BlipColor.Blue)))
                    b.Remove();
                foreach (
                    var v in
                        World.GetAllVehicles().Where(v => (v.GetPedOnSeat(VehicleSeat.Driver).Model == "A_C_Chimp")))
                    v.Delete();
                _activateMod = true;
                _bus = World.CreateVehicle("Bus", _busPosition);
                _bus.CreatePedOnSeat(VehicleSeat.Driver, new Model("A_C_Chimp"));
                _busBlip = _bus.AddBlip();
                _busBlip.Color = BlipColor.Blue;
                _busBlip.Name = "Bus";
                _busBlip.ShowRoute = true;
                _busBlip.IsFriendly = true;
                _driver = _bus.GetPedOnSeat(VehicleSeat.Driver);
            }
        }
    }
}
