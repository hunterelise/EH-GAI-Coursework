using UnityEngine;

public class TestingVariables : MonoBehaviour
{
	/// <summary>If >= 0 then the specified seed will be used for terrain generation rather than the normal algorithms</summary>
	public const int TerrainSeed = -1;

	/// <summary>If >= 0 then the specified seed will be used for unit locations rather than the normal algorithms</summary>
	public const int LocationsSeed = -1;

	/// <summary>If < 0 then normal amount of enemies are produced. If >= 0 then the specific amount of enemeies are created up to the allowed normal amount</summary>
	public const int MaxEnemies = -1;

	/// <summary>If < 0 then normal amount of allies are produced. If >= 0 then the specific amount of allies are created up to the allowed normal amount</summary>
	public const int MaxAllies = -1;


	/// Please only change the colours if you have difficulty seeing the distinction between the default colours
	public static readonly Color ColourWater = Color.blue;
	public static readonly Color ColourMud = new Color(130f / 255f, 60f / 255f, 10f / 255f);
	public static readonly Color ColourGrass = new Color(0f, 0.8f, 0f);
	public static readonly Color ColourTrees = new Color(0f, 0.4f, 0f);
	public static readonly Color ColourSnow = Color.white;
	public static readonly Color ColourUnknown = Color.magenta;
	public static readonly Color ColourInvalid = Color.black;
	public static readonly Color ColourAlly = Color.magenta;
	public static readonly Color ColourEnemy = Color.yellow;

}
