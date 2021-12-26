using System;
using System.Collections;
using UnityEngine;

namespace UnturnedLegends.Structs
{
    public class FFAPlayer
    {
        public GamePlayer GamePlayer { get; set; }

        public int Kills { get; set; }
        public int KillStreak { get; set; }
        public int MultipleKills { get; set; }

        public bool HasSpawnProtection { get; set; }

        public DateTime LastKill { get; set; }
        public DateTime LastDamage { get; set; }

        public Coroutine ProtectionRemover { get; set; }
        public Coroutine DamageChecker { get; set; }
        public Coroutine Healer { get; set; }

        public FFAPlayer(GamePlayer gamePlayer)
        {
            GamePlayer = gamePlayer;

            Kills = 0;
            KillStreak = 0;
            MultipleKills = 0;
            LastKill = DateTime.UtcNow;
        }

        public void OnDeath()
        {
            KillStreak = 0;
            MultipleKills = 0;
            LastKill = DateTime.UtcNow;
        }

        public void GiveSpawnProtection()
        {
            HasSpawnProtection = true;
            if (ProtectionRemover != null)
            {
                Plugin.Instance.StopCoroutine(ProtectionRemover);
            }
            Plugin.Instance.StartCoroutine(RemoveSpawnProtection());
        }

        public IEnumerator RemoveSpawnProtection()
        {
            yield return new WaitForSeconds(Plugin.Instance.Configuration.Instance.FFA.SpawnProtectionSeconds);
            HasSpawnProtection = false;
        }

        // Push this to the GamePlayer Class later on, but it doesn't do anything as of now.
        public void OnDamaged()
        {
            Utility.Debug($"{GamePlayer.Player.CharacterName} got damaged, setting the last damage to now and checking after some seconds to heal");
            LastDamage = DateTime.UtcNow;
            if (DamageChecker != null)
            {
                Plugin.Instance.StopCoroutine(DamageChecker);
            }
            if (Healer != null)
            {
                Plugin.Instance.StopCoroutine(Healer);
            }

            DamageChecker = Plugin.Instance.StartCoroutine(CheckDamage());
        }

        public IEnumerator CheckDamage()
        {
            yield return new WaitForSeconds(Plugin.Instance.Configuration.Instance.LastDamageAfterHealSeconds);
            Utility.Debug($"Starting healing on {GamePlayer.Player.CharacterName}");
            Healer = Plugin.Instance.StartCoroutine(HealPlayer());
        }

        public IEnumerator HealPlayer()
        {
            var seconds = Plugin.Instance.Configuration.Instance.HealSeconds;
            while (true)
            {
                yield return new WaitForSeconds(seconds);
                var health = GamePlayer.Player.Player.life.health;
                if (health == 100)
                {
                    break;
                }
                GamePlayer.Player.Player.life.serverModifyHealth(1);
            }
        }
        //

        public void Destroy()
        {
            if (DamageChecker != null)
            {
                Plugin.Instance.StopCoroutine(DamageChecker);
            }
            if (Healer != null)
            {
                Plugin.Instance.StopCoroutine(Healer);
            }
        }
    }
}
