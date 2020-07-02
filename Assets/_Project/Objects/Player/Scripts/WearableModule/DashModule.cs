using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDashModule", menuName = "Player/Modules/Wearable/Dash", order = -1)]
public class DashModule : BasePlayerModule, WearableModule {

    [Header("Parameters")]
    public float maxDashLength = 0.075f;
    public float dashSpeed = 40f;
    public float additionalImpulse = 6f;
    public float hitKnockback = 42f;

    public void UpdateStatusPMW (PlayerInfo info, PlayerModifierData data) {
    }

    public void UpdateActionPMW (PlayerInfo info, PlayerModifierData data) {
        float pressLength = Time.time - ((DashModifierData)data).timeOfInitialPress;
        if(((DashModifierData)data).dashStarted) {
            ((DashModifierData)data).dashStarted = false;

            info.pc.TickTrail(0);

            info.rbody.velocity += ((DashModifierData)data).dir * additionalImpulse;
        }
        if(pressLength < maxDashLength && info.pc.rbody.submergedPercentage > 0.5f) {
            float motionFactor = dashSpeed * Time.deltaTime;
            info.rbody.MoveByDelta(((DashModifierData)data).dir * motionFactor);

            info.pc.TickTrail(0);

            EntityManager.inst.ExecuteOverlapsEntity(
            info.rbody.aabb, (entity) => {
                Vector2 idir = Vector2.Lerp(((DashModifierData)data).dir, ((Vector2)entity.transform.position - info.pc.GetCenterPosition()).normalized, 0.5f).normalized * 42f;
                Vector2 vel = ((LivingEntity)entity).rigidbody.velocity;
                ((LivingEntity)entity).rigidbody.velocity = new Vector2(
                        idir.x > 0f ? Mathf.Max(idir.x, vel.x) : Mathf.Min(idir.x, vel.x),
                        idir.y > 0f ? Mathf.Max(idir.y, vel.y) : Mathf.Min(idir.y, vel.y)
                    );

                if(!((LivingEntity)entity).GetEffect("default:stunned", out GenericEffect effect)) {
                    ParticleManager.inst.PlayFixedParticle((Vector2)((LivingEntity)entity).transform.position, 9);
                    ((LivingEntity)entity).HitEntity(0f);
                    ((LivingEntity)entity).ApplyEffect("default:stunned", 2.5f, 0);
                }
            }
        );
        }
    }
}
