# Terraria Crafting Table For Survivalcraft 泰拉工作台，但在生存战争

This mod adds a new block: the Terraria Crafting Table.  
When you interact with the Terraria Crafting Table, you can directly craft items without placing ingredients in the slots. The behavior is similar to the game
*Terraria*.  
The ingredients are taken from your inventory and chests connected to the Terraria Crafting Table. Chests further away can be connected to the Terraria Crafting Table through other chests.  
The crafting results will be placed into the player's inventory.

本模组添加了一个新方块：泰拉工作台  
玩家与泰拉工作台交互后，可在弹出对话框像游戏《泰拉瑞亚》那样直接合成物品，而无需摆放原料  
原料来源是玩家背包，以及与泰拉工作台相连的箱子，更远处的箱子可通过箱子与泰拉工作台相连  
合成结果将放入玩家背包

## Notes 注意

* Items requiring fuel cannot be crafted.
* Furniture will be rejected for crafting or used as ingredients.
* Items in the creative mode inventory will not be used as ingredients.
* Crafting tables, furnaces, and dispensers are also considered as chests. Results stored in crafting tables and furnaces will also be treated as ingredients. If a block entity from mods has a `Component` inherited from `ComponentInventoryBase`, this block entity will also be considered as a chest.
* When the items in the inventory or connected chests are changed, the dialog will not update automatically.
* Some mods may contain extremely special crafting recipes. The maximum crafting result count displayed in the dialog might be incorrect, but rest assured, this mod will never allow players to get crafting result more than they can get.
* To ensure no over-crafting, this mod will remove ingredients first. If the ingredients are not enough, they will be returned to their original inventory/chest. If there are new items added to the original inventory/chest during this process, the ingredients which cannot be returned will be thrown upward.
* If the crafting results cannot be placed to the player's inventory, the left ingredients will be thrown upward too.
* This mod supports extended inventory mod of up to 6x6 slots. And the additional slots beyond this limit will not be displayed in the dialog.
* This mod gets crafting recipes from the `CraftingRecipesManager` one second after loading a world. The following issues may appear:
    * If other mods dynamically add, modify, or delete recipes in `CraftingRecipesManager` during gameplay, this mod will not refresh the cached recipes, and will continue using the cached recipes.
    * If other mods use `CraftingRecipesManager` and add additional crafting restrictions, this mod is unaware and will not apply these restrictions.


* 不能合成需要燃料的物品
* 家具会被拒绝合成、或当作原料
* 创造模式背包的物品不会被当作原料
* 合成台、熔炉、发射器也算箱子，合成台和熔炉里的结果也将被当作原料；如果其中模组的方块实体，具有从`ComponentInventoryBase`继承的`Component`，该方块实体也会被视为箱子
* 背包、箱子内物品变化时不会自动更新对话框
* 可能存在一些模组含有极其特殊的合成表，本模组显示的最大合成数量可能有误，但请放心，绝对不会让玩家多合成
* 本模组为确保不多合成，会先扣除原料，再进行合成，原料不足会原路退回；如果期间原箱子存入了新的方块，导致无法退回，会向上方抛出原料
* 如果玩家背包不足以存放合成结果，会向上抛出原料
* 本模组兼容最大 6*6 格的大背包，超出的格子将不显示
* 本模组在每次进入存档后 1 秒时从游戏的`CraftingRecipesManager`读取合成表，可能存在以下问题：
    * 如果其他模组在游戏进行时动态添加、修改、删除`CraftingRecipesManager`中的合成表，本模组不会刷新合成表缓存，会继续按缓存的合成表进行合成
    * 如果其他模组使用`CraftingRecipesManager`但添加了其他合成限制条件，本模组不知晓也不会限制这些合成

## Icon 图标

> By ChatGPT 由ChatGPT创作

![Icon 图标](https://github.com/XiaofengdiZhu/TerrariaCraftingTableForSC/blob/main/OriginalIcon.webp?raw=true)