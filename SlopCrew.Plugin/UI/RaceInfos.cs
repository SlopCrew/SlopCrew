using SlopCrew.Common.Race;
using SlopCrew.Plugin.Scripts;
using System.Linq;
using UnityEngine;

namespace SlopCrew.Plugin.UI {
    //TODO: temporary
    internal class RaceInfos : MonoBehaviour {
        private readonly GUIStyle style = new GUIStyle {
            fontSize = 50,
            normal = { textColor = Color.white }
        };

        private float labelWidth = 100f;
        private float labelHeight = 100f;
        private float xPercent = 0.5f;
        private float yPercent = 0.1f;

        private GUIStyle GetStyle(int fontSize = 50) {
            return new GUIStyle {
                fontSize = fontSize,
                normal = { textColor = Color.white }
            };
        }

        public void OnGUI() {

            RaceState state = RaceManager.Instance.GetState();

            switch (state) {
                case RaceState.None:
                    break;
                case RaceState.WaitingForRace:
                    GUI.Label(new Rect(
                            Screen.width * xPercent - labelWidth / 2,
                            Screen.height * yPercent - labelHeight / 2,
                            labelWidth,
                            labelHeight),
                            "Waiting for a race...",
                            GetStyle());

                    break;
                case RaceState.WaitingForPlayers:
                    GUI.Label(new Rect(
                            Screen.width * xPercent - labelWidth / 2,
                            Screen.height * yPercent - labelHeight / 2,
                            labelWidth,
                            labelHeight),
                            "Waiting for players to join ...",
                            GetStyle());
                    break;
                case RaceState.WaitingForPlayersToBeReady:
                    GUI.Label(new Rect(
                            Screen.width * xPercent - labelWidth / 2,
                            Screen.height * yPercent - labelHeight / 2,
                            labelWidth,
                            labelHeight),
                            "Waiting for players to initialize race...",
                            GetStyle());
                    break;
                case RaceState.Starting:
                    var time = GetTimeFrom();
                    GUI.Label(new Rect(
                            Screen.width * xPercent - labelWidth / 2,
                            Screen.height * yPercent - labelHeight / 2,
                            labelWidth,
                            labelHeight),
                            time,
                            GetStyle());
                    break;
                case RaceState.Racing:
                case RaceState.Finished:
                case RaceState.WaitingForFullRanking:
                    GUI.Label(new Rect(10, 50, 500, 500), GetTime(), GetStyle());
                    break;
                case RaceState.ShowRanking:
                    var text = "";
                    var ranks = RaceManager.Instance.GetRank();
                    for (int i = 0; i < ranks.Count(); i++) {
                        (var name, var playerTime) = ranks.ElementAt(i);

                        text += $"{i + 1} - {name}: {playerTime}" + "\n";
                    }


                    GUI.Label(new Rect(10, 10, labelWidth, labelHeight), text, GetStyle(30));
                    break;
                default:
                    break;
            }

        }

        private string GetTime() {
            var raceManager = RaceManager.Instance;

            var time = raceManager.GetTime();
            var minutes = Mathf.FloorToInt(time / 60);
            var seconds = Mathf.FloorToInt(time % 60);
            var fraction = Mathf.FloorToInt((time * 100) % 100);

            return $"{minutes:00}:{seconds:00}:{fraction:00}";
        }

        private string GetTimeFrom(int from = 4) {
            var raceManager = RaceManager.Instance;

            var time = raceManager.GetTime();
            var minutes = Mathf.FloorToInt(from - time);

            return $"{minutes:00}";
        }
    }
}
