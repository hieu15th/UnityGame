public class Npc
{
    public int id;
    public string name;
    public float x, y;

    public int head;
    public int facehair;
    public int helmet;
    public int hair;
    public int body;
    public int armor;
    public int hand;
    public int leg;
    public int boot;
    public int weapon;
    public int cloak;

    public Npc(int id, string name, float x, float y,
        int head, int facehair, int helmet, int hair, int body, int armor,
        int hand, int leg, int boot, int weapon, int cloak)
    {
        this.id = id;
        this.name = name;
        this.x = x;
        this.y = y;
        this.head = head;
        this.facehair = facehair;
        this.helmet = helmet;
        this.hair = hair;
        this.body = body;
        this.armor = armor;
        this.hand = hand;
        this.leg = leg;
        this.boot = boot;
        this.weapon = weapon;
        this.cloak = cloak;
    }
}
