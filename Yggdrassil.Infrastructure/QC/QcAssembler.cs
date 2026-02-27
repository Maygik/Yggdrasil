using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.QC;

namespace Yggdrassil.Infrastructure.QC
{
    /// <summary>
    /// Assembles QC text by combining templates, generated blocks and conditional sections based on the QcConfig and project settings.
    /// </summary>
    public sealed class QcAssembler : IQcAssembler
    {
        private QcTemplateStore _qcTemplateStore;

        public QcAssembler(QcTemplateStore store)
        {
            _qcTemplateStore = store;
        }


        public string AssembleQc(QcConfig config)
        {
            // Make sure the config has meshes
            if (config.Bodygroups == null || config.Bodygroups.Count == 0 || config.Bodygroups.Any(bg => bg.Submeshes == null || bg.Submeshes.Count == 0))
            {
                throw new InvalidOperationException("QcConfig must have at least one bodygroup with meshes defined.");
            }
            var firstBodygroup = config.Bodygroups.First();
            if (firstBodygroup.Submeshes == null || firstBodygroup.Submeshes.Count == 0)
            {
                throw new InvalidOperationException("First bodygroup must have at least one submesh.");
            }


            var sb = new StringBuilder();

            // Start with the core template which includes the basic structure and common directives
            // The core template will have placeholders for model path, materials, bodygroups, etc. that we replace using the QcPlaceholderReplacer
            sb.Append(QcPlaceholderReplacer.ReplacePlaceholders(_qcTemplateStore.Get("core"), config));

            sb.AppendLine();
            sb.AppendLine();


            if (config.AnimationProfile != AnimationProfile.None && config.AnimationProfile != AnimationProfile.RagdollOnly)
            {
                sb.Append(QcPlaceholderReplacer.ReplacePlaceholders(_qcTemplateStore.Get("ik"), config));
            }

            sb.AppendLine();
            sb.AppendLine();

            // Then we add the animation block based on the selected animation profile in the config
            switch (config.AnimationProfile)
            {
                case AnimationProfile.RagdollOnly:
                    sb.Append(_qcTemplateStore.Get("anim_ragdoll"));
                    break;
                case AnimationProfile.MalePlayer:
                    sb.Append(_qcTemplateStore.Get("anim_male"));
                    break;
                case AnimationProfile.FemalePlayer:
                    sb.Append(_qcTemplateStore.Get("anim_female"));
                    break;
                case AnimationProfile.MaleNPC:
                    sb.Append(_qcTemplateStore.Get("anim_male_npc"));
                    break;
                case AnimationProfile.FemaleNPC:
                    sb.Append(_qcTemplateStore.Get("anim_female_npc"));
                    break;
                case AnimationProfile.CombineNPC:
                    sb.Append(_qcTemplateStore.Get("anim_combine_npc"));
                    break;
                case AnimationProfile.MetrocopNPC:
                    sb.Append(_qcTemplateStore.Get("anim_metrocop_npc"));
                    break;
                case AnimationProfile.None:
                    var firstMeshName = firstBodygroup.Submeshes[0];
                    sb.Append($"$sequence \"idle\" \"{firstMeshName}\"");
                    break;
            }

            sb.AppendLine();
            sb.AppendLine();

            // Next, we conditionally add blocks based on the features enabled in the config
            /*
            if (config.Features.UseIk)
            {
                sb.Append(QcPlaceholderReplacer.ReplacePlaceholders(_qcTemplateStore.Get("ik"), config));
            }
            */


            sb.AppendLine();

            if (config.Features.UseHitboxes)
            {
                sb.Append(QcPlaceholderReplacer.ReplacePlaceholders(_qcTemplateStore.Get("hitbox"), config));
            }

            sb.AppendLine();
            sb.AppendLine();

            // TODO: Add more conditional blocks based on other features and settings in QcConfig
            // e.g. jigglebones, custom bodygroups, etc.

            return sb.ToString();
        }
    }
}
