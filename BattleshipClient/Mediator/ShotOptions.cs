namespace BattleshipClient.Mediator
{
    public readonly record struct ShotOptions(
        bool DoubleBomb,
        bool PlusShape,
        bool XShape,
        bool SuperDamage
    );
}
