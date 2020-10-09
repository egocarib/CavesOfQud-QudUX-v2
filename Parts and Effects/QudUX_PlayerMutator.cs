using XRL;
using XRL.World;
namespace XRL.World.Parts
{
  [PlayerMutator]
  public class QudUX_PlayerMutator : IPlayerMutator
  {
      public void mutate(GameObject player)
      {
          // add your command listener to the player when a New Game begins
          player.AddPart<QudUX_CommandListener>();
      }
  }
}
