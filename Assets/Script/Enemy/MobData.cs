using UnityEngine;

public class MobData : MonoBehaviour
{
    public int id;
    public string mobName;
    public int current_hp;
    public int hp;
    public int dame;
    public int part;
    public int hp_del;
    public bool attack=false;
    public void ApplyFrom(Mobs mob)
    {
        id = mob.id;
        mobName = mob.name;
        current_hp = mob.current_hp;
        hp = mob.hp;
        dame = mob.dame;
        part = mob.part;
    }

    public override string ToString()
    {
        return $"Mob[{id}] {mobName} | HP: {current_hp}/{hp} | Dame: {dame} | Part: {part}";
    }
}
