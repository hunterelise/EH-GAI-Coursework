using UnityEngine;

public struct Attack
{
	public enum AttackType
	{
		None,
		Melee,
		AllyGun,
		EnemyGun,
		Rocket,
		Explosion
	}

	public class Data
	{
		public readonly float damage;
		public readonly float radius;
		public readonly float speed;
		public readonly float range;
		public readonly bool friendlyFire;
		public readonly bool oneHit;
		public readonly string spriteName;

		public Data(float damage, float radius, float speed, float range, bool friendlyFire, bool oneHit, string spriteName)
		{
			this.damage = damage;
			this.radius = radius;
			this.speed = speed;
			this.range = range;
			this.friendlyFire = friendlyFire;
			this.oneHit = oneHit;
			this.spriteName = spriteName;
		}
	}

	public static readonly Data NoneData = new Data(0.0f, 0.0f, 0.0f, 0.0f, false, false, null);
	public static readonly Data MeleeData = new Data(1.0f, SteeringAgent.CollisionRadius, 0.1f, 0.02f, false, true, "Melee");
	public static readonly Data AllyGunData = new Data(0.25f, 0.125f, 15.0f, 15.0f, false, true, "Bullet");
	public static readonly Data EnemyGunData = new Data(0.15f, 0.125f, 10.0f, 14.0f, false, true, "Bullet");
	public static readonly Data RocketData = new Data(0.1f, 0.25f, 8.0f, 30.0f, false, true, "Rocket");

	/// <summary>
	/// Explosions have a speed and range due to not implementing timing attack information. Therefore, to get explosions to visually
	/// appear for a little time duration they actually move a very small amount until their range is triggered where they will cease to
	/// exist. Obviously not the best way of doing this but it gives the effect that is wanted though it does make explosions deadly!
	/// </summary>
	public static readonly Data ExplosionData = new Data(1.0f, 1.5f, 0.01f, 0.01f * 0.5f, true, false, "Explosion");
	public static readonly Data[] AttackDatas = new Data[]
	{
		NoneData,
		MeleeData,
		AllyGunData,
		EnemyGunData,
		RocketData,
		ExplosionData
	};



	public float Damage => AttackDatas[(int)Type].damage;
	public float Radius => AttackDatas[(int)Type].radius;
	public float Speed => AttackDatas[(int)Type].speed;
	public float Range => AttackDatas[(int)Type].range;
	public bool FriendlyFire => AttackDatas[(int)Type].friendlyFire;
	public bool OneHit => AttackDatas[(int)Type].oneHit;
	public string SpriteName => AttackDatas[(int)Type].spriteName;

	public readonly AttackType Type;
	public readonly bool IsEnemy;
	public readonly SteeringAgent AttackerAgent;
	public readonly GameObject AttackGO;
	public readonly Vector3 Direction;
	public readonly Vector3 StartPosition;
	public Vector3 currentPosition;

	public Attack(AttackType attackType, GameObject attackGO, SteeringAgent attackerAgent)
	{
		Type = attackType;
		IsEnemy = attackerAgent.GetComponent<EnemyAgent>() != null;
		AttackGO = attackGO;
		AttackerAgent = attackerAgent;
		Direction = Vector3.Normalize(attackGO.transform.up);
		StartPosition = attackGO.transform.position;
		currentPosition = attackGO.transform.position;
	}
}
