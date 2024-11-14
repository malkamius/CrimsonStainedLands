using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    public static class Dueling
    {

        public static bool DuelBlocking(Character character) => character.Flags.ISSET(ActFlags.NoDuels);
        public static bool DuelPending(Character character) => 
            character.AffectsList.Any(aff => aff.flags.ISSET(
                AffectFlags.DuelChallenge, 
                AffectFlags.DuelChallenged, 
                AffectFlags.DuelStarting,
                AffectFlags.DuelInProgress,
                AffectFlags.DuelCancelling));

        public static void DoIssueDuelChallenge(Character character, string arguments)
        {
            if (character.GetCharacterFromListByName(
                    from player 
                    in Game.Instance.Info.Connections 
                    where player.state == Player.ConnectionStates.Playing
                    select player, 
                arguments, out var victim, Character.GetFlags.PlayerName, Character.GetFlags.DisallowStringPrefix))
            {
                if (DuelBlocking(victim))
                {
                    character.send("They aren't accepting duels right now.\r\n");
                }
                else if (DuelPending(victim))
                {
                    character.send("They already have a duel challenge.");
                }
                else
                {
                    character.Act("You issue a duel challenge to $N!", victim);
                    character.Act("$n issues a duel challenge to $N!", victim, type: ActType.GlobalNotVictim);
                    character.Act("$n issues a duel challenge to you!", victim, type: ActType.ToVictim);

                    var affect = new AffectData();
                    affect.flags.SETBIT(AffectFlags.DuelChallenged);
                    affect.ownerName = character.Name;
                    affect.duration = 5;
                    affect.frequency = Frequency.Tick;
                    affect.displayName = "Challenge from " + affect.ownerName;
                    affect.endMessage = "You did not accept the challenge.";
                    affect.RemoveAndSaveFlags.SETBIT(AffectData.StripAndSaveFlags.DoNotSave);
                    affect.hidden = false;

                    victim.AffectToChar(affect);

                    affect = new AffectData(affect);
                    affect.ownerName = victim.Name;
                    affect.flags.Clear();
                    affect.flags.Add(AffectFlags.DuelChallenge);
                    affect.RemoveAndSaveFlags.SETBIT(AffectData.StripAndSaveFlags.DoNotSave);
                    affect.displayName = "Challenge to " + affect.ownerName;
                    affect.endMessage = "Your challenge was not accepted.";
                    character.AffectToChar(affect);
                }
            }
            else
                character.send("You don't see them here. (You must specify their entire unaltered name)\r\n");
        }

        public static void DoDuelAccept(Character character, string arguments)
        {
            
            if (character.FindAffect(AffectFlags.DuelChallenged, out var affect))
            {
                var challenger = Character.Characters.Where(c => !c.IsNPC && c.Name == affect.ownerName).FirstOrDefault();

                if(challenger == null)
                {
                    character.send("Your challenger doesn't seem to be around anymore.\r\n");
                }
                else
                {
                    challenger.StripAffect(AffectFlags.DuelChallenge, true);
                    character.StripAffect(AffectFlags.DuelChallenged, true);

                    var newaffect = new AffectData();
                    newaffect.flags.SETBIT(AffectFlags.DuelStarting);
                    newaffect.ownerName = character.Name;
                    newaffect.duration = 5;
                    newaffect.frequency = Frequency.Violence;
                    newaffect.displayName = "Duel starting: " + newaffect.ownerName;
                    newaffect.tickProgram = "DuelStartingProgram";
                    newaffect.endProgram = "DuelStartProgram";
                    newaffect.hidden = false;
                    newaffect.RemoveAndSaveFlags.SETBIT(AffectData.StripAndSaveFlags.DoNotSave);
                    challenger.AffectToChar(newaffect);

                    newaffect = new AffectData(newaffect);
                    newaffect.ownerName = challenger.Name;
                    newaffect.displayName = "Duel starting: " + newaffect.ownerName;
                    newaffect.RemoveAndSaveFlags.SETBIT(AffectData.StripAndSaveFlags.DoNotSave);
                    character.AffectToChar(newaffect);
                }
            }
            else
                character.send("You have not been challenged.\r\n");
        }

        public static void DoDuelDecline(Character character, string arguments)
        {

            if (character.FindAffect(AffectFlags.DuelChallenged, out var affect))
            {
                var challenger = Character.Characters.Where(c => !c.IsNPC && c.Name == affect.ownerName).FirstOrDefault();

                if (challenger == null)
                {
                    character.send("Your challenger doesn't seem to be around anymore.\r\n");
                }
                else
                {
                    challenger.StripAffect(AffectFlags.DuelChallenge);
                }
                character.StripAffect(AffectFlags.DuelChallenged);
            }
            else
                character.send("You have not been challenged.\r\n");
        }
    }
}
