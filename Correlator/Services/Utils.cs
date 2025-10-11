using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventProcessor.Models;

namespace Correlator.Services
{
    public static class Utils
    {
        public static string DetermineAlertType(List<RawEventDto> events)
        {
            // Variables para controlar la presencia de ciertos tipos de eventos
            bool hasPanicButton = false;
            bool hasSpeedSensor = false;
            bool hasAcousticSensor = false;
            bool hasCriticalEvents = false;

            foreach (var e in events)
            {
                if (e.event_type == "panic.button")
                {
                    hasPanicButton = true;
                }

                if (e.event_type == "sensor.speed")
                {
                    hasSpeedSensor = true;
                }

                if (e.event_type == "sensor.acoustic")
                {
                    hasAcousticSensor = true;
                }

                if (e.severity == "critical")
                {
                    hasCriticalEvents = true;
                }
            }

            if (hasPanicButton && hasAcousticSensor)
            {
                return "possible_robbery";
            }
            else if (hasSpeedSensor && hasAcousticSensor)
            {
                return "accident";
            }
            else if (hasAcousticSensor && hasCriticalEvents)
            {
                return "fire";
            }
            else if (hasPanicButton)
            {
                return "emergency";
            }
            else
            {
                return "general_alert";
            }
        }

        public static double CalculateAlertScore(List<RawEventDto> events)
        {
            int eventCount = events.Count;
            double severityScore = 0;
            int criticalEventCount = 0;

            foreach (var e in events)
            {
                if (e.severity == "critical")
                {
                    severityScore += 1.0;
                    criticalEventCount++;
                }
                else if (e.severity == "warning")
                {
                    severityScore += 0.7;
                }
                else if (e.severity == "info")
                {
                    severityScore += 0.3;
                }
            }

            double eventCountScore = (eventCount >= 5) ? 1.0 : (eventCount / 5.0);

            double score = (severityScore / eventCount) * 0.7 + eventCountScore * 0.3;

            score = Math.Min(1.0, score);

            return score;
        }
    }
}
