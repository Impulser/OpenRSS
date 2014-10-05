using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

using OpenRSS.Extensions;
using RSUtilities;

namespace OpenRSS.Cache.Definition
{
    /// <summary>
    ///     A class that loads item/model information from the cache.
    ///     @author Graham
    ///     @author `Discardedx2
    ///     TODO Finish some of the opcodes.
    /// </summary>
    public class ItemDefinition
    {
        private int colourEquip1;

        private int colourEquip2;

        private int femaleWearModel1;

        private int femaleWearModel2;

        private string[] groundOptions;

        private int inventoryModelId;

        private string[] inventoryOptions;

        private int lendId;

        private int lendTemplateId;

        private int maleWearModel1;

        private int maleWearModel2;

        private bool membersOnly;

        private int modelOffset1;

        private int modelOffset2;

        private int modelRotation1;

        private int modelRotation2;

        private int modelZoom;

        private short[] modifiedModelColors;

        private string name;

        private int notedId;

        private int notedTemplateId;

        private short[] originalModelColors;

        private int stackable;

        private int[] stackableAmounts;

        private int[] stackableIds;

        private int teamId;

        private short[] textureColour1;

        private short[] textureColour2;

        private bool unnoted;

        private int value;

        /// <param name="buffer">
        ///     A <seealso cref="ByteBuffer" /> that contains information
        ///     such as the items location.
        /// </param>
        /// <returns> a new ItemDefinition. </returns>
        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @SuppressWarnings("unused") public static ItemDefinition decode(java.nio.ByteBuffer buffer)
        public static ItemDefinition Decode(ByteBuffer buffer)
        {
            var def = new ItemDefinition();
            def.groundOptions = new[]
            {
                null,
                null,
                "take",
                null,
                null
            };
            def.inventoryOptions = new[]
            {
                null,
                null,
                null,
                null,
                "drop"
            };
            while (true)
            {
                var opcode = buffer.get() & 0xFF;
                if (opcode == 0)
                {
                    break;
                }
                if (opcode == 1)
                {
                    def.inventoryModelId = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 2)
                {
                    def.name = ByteBufferExtensions.GetJagexString(buffer);
                }
                else if (opcode == 4)
                {
                    def.modelZoom = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 5)
                {
                    def.modelRotation1 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 6)
                {
                    def.modelRotation2 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 7)
                {
                    def.modelOffset1 = buffer.getShort() & 0xFFFFF;
                    if (def.modelOffset1 > 32767)
                    {
                        def.modelOffset1 -= 65536;
                    }
                    def.modelOffset1 <<= 0;
                }
                else if (opcode == 8)
                {
                    def.modelOffset2 = buffer.getShort() & 0xFFFFF;
                    if (def.modelOffset2 > 32767)
                    {
                        def.modelOffset2 -= 65536;
                    }
                    def.modelOffset2 <<= 0;
                }
                else if (opcode == 11)
                {
                    def.stackable = 1;
                }
                else if (opcode == 12)
                {
                    def.value = buffer.getInt();
                }
                else if (opcode == 16)
                {
                    def.membersOnly = true;
                }
                else if (opcode == 23)
                {
                    def.maleWearModel1 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 24)
                {
                    def.femaleWearModel1 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 25)
                {
                    def.maleWearModel2 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 26)
                {
                    def.femaleWearModel2 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode >= 30 && opcode < 35)
                {
                    def.groundOptions[opcode - 30] = buffer.GetJagexString();
                }
                else if (opcode >= 35 && opcode < 40)
                {
                    def.inventoryOptions[opcode - 35] = buffer.GetJagexString();
                }
                else if (opcode == 40)
                {
                    var length = buffer.get() & 0xFF;
                    def.originalModelColors = new short[length];
                    def.modifiedModelColors = new short[length];
                    for (var index = 0; index < length; index++)
                    {
                        def.originalModelColors[index] = unchecked((short) (buffer.getShort() & 0xFFFFF));
                        def.modifiedModelColors[index] = unchecked((short) (buffer.getShort() & 0xFFFFF));
                    }
                }
                else if (opcode == 41)
                {
                    var length = buffer.get() & 0xFF;
                    def.textureColour1 = new short[length];
                    def.textureColour2 = new short[length];
                    for (var index = 0; index < length; index++)
                    {
                        def.textureColour1[index] = unchecked((short) (buffer.getShort() & 0xFFFFF));
                        def.textureColour2[index] = unchecked((short) (buffer.getShort() & 0xFFFFF));
                    }
                }
                else if (opcode == 42)
                {
                    var length = buffer.get() & 0xFF;
                    for (var index = 0; index < length; index++)
                    {
                        int i = buffer.get();
                    }
                }
                else if (opcode == 65)
                {
                    def.unnoted = true;
                }
                else if (opcode == 78)
                {
                    def.colourEquip1 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 79)
                {
                    def.colourEquip2 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 90)
                {
                    var i = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 91)
                {
                    var i = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 92)
                {
                    var i = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 93)
                {
                    var i = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 95)
                {
                    var i = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 96)
                {
                    var i = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 97)
                {
                    def.notedId = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 98)
                {
                    def.notedTemplateId = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode >= 100 && opcode < 110)
                {
                    if (def.stackableIds == null)
                    {
                        def.stackableIds = new int[10];
                        def.stackableAmounts = new int[10];
                    }
                    def.stackableIds[opcode - 100] = buffer.getShort() & 0xFFFFF;
                    def.stackableAmounts[opcode - 100] = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 110)
                {
                    var i = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 111)
                {
                    var i = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 112)
                {
                    var i = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 113)
                {
                    int i = buffer.get();
                }
                else if (opcode == 114)
                {
                    var i = buffer.get() * 5;
                }
                else if (opcode == 115)
                {
                    def.teamId = buffer.get() & 0xFF;
                }
                else if (opcode == 121)
                {
                    def.lendId = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 122)
                {
                    def.lendTemplateId = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 125)
                {
                    var i = buffer.get() << 0;
                    var i2 = buffer.get() << 0;
                    var i3 = buffer.get() << 0;
                }
                else if (opcode == 126)
                {
                    var i = buffer.get() << 0;
                    var i2 = buffer.get() << 0;
                    var i3 = buffer.get() << 0;
                }
                else if (opcode == 127)
                {
                    var i = buffer.get() & 0xFF;
                    var i2 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 128)
                {
                    var i = buffer.get() & 0xFF;
                    var i2 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 129)
                {
                    var i = buffer.get() & 0xFF;
                    var i2 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 130)
                {
                    var i = buffer.get() & 0xFF;
                    var i2 = buffer.getShort() & 0xFFFFF;
                }
                else if (opcode == 132)
                {
                    var len = buffer.get() & 0xFF;
                    for (var index = 0; index < len; index++)
                    {
                        var anInt = buffer.getShort() & 0xFFFFF;
                    }
                }
                else if (opcode == 249)
                {
                    var length = buffer.get() & 0xFF;
                    for (var index = 0; index < length; index++)
                    {
                        var stringInstance = buffer.get() == 1;
                        var key = ByteBufferExtensions.GetTriByte(buffer);
                        var value = stringInstance ? (object) ByteBufferExtensions.GetJagexString(buffer) : buffer.getInt();
                    }
                }
            }
            return def;
        }

        public virtual string GetName()
        {
            return name;
        }

        public virtual int GetInventoryModelId()
        {
            return inventoryModelId;
        }

        public virtual int GetModelZoom()
        {
            return modelZoom;
        }

        public virtual int GetModelRotation1()
        {
            return modelRotation1;
        }

        public virtual int GetModelRotation2()
        {
            return modelRotation2;
        }

        public virtual int GetModelOffset1()
        {
            return modelOffset1;
        }

        public virtual int GetModelOffset2()
        {
            return modelOffset2;
        }

        public virtual int GetStackable()
        {
            return stackable;
        }

        public virtual int GetValue()
        {
            return value;
        }

        public virtual bool IsMembersOnly()
        {
            return membersOnly;
        }

        public virtual int GetMaleWearModel1()
        {
            return maleWearModel1;
        }

        public virtual int GetMaleWearModel2()
        {
            return maleWearModel2;
        }

        public virtual int GetFemaleWearModel1()
        {
            return femaleWearModel1;
        }

        public virtual int GetFemaleWearModel2()
        {
            return femaleWearModel2;
        }

        public virtual string[] GetGroundOptions()
        {
            return groundOptions;
        }

        public virtual string[] GetInventoryOptions()
        {
            return inventoryOptions;
        }

        public virtual short[] GetOriginalModelColors()
        {
            return originalModelColors;
        }

        public virtual short[] GetModifiedModelColors()
        {
            return modifiedModelColors;
        }

        public virtual short[] GetTextureColour1()
        {
            return textureColour1;
        }

        public virtual short[] GetTextureColour2()
        {
            return textureColour2;
        }

        public virtual bool IsUnnoted()
        {
            return unnoted;
        }

        public virtual int GetColourEquip1()
        {
            return colourEquip1;
        }

        public virtual int GetColourEquip2()
        {
            return colourEquip2;
        }

        public virtual int GetNotedId()
        {
            return notedId;
        }

        public virtual int GetNotedTemplateId()
        {
            return notedTemplateId;
        }

        public virtual int[] GetStackableIds()
        {
            return stackableIds;
        }

        public virtual int[] GetStackableAmounts()
        {
            return stackableAmounts;
        }

        public virtual int GetTeamId()
        {
            return teamId;
        }

        public virtual int GetLendId()
        {
            return lendId;
        }

        public virtual int GetLendTemplateId()
        {
            return lendTemplateId;
        }
    }
}
