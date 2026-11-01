namespace BVN.Flash
{
    /// <summary>Flash-style mouse targeting: topmost visible content under the point, promoted to the
    /// nearest interactive ancestor honouring mouseEnabled / mouseChildren.</summary>
    public static class MouseHit
    {
        public static DisplayObject InteractiveAt(Stage stage, double x, double y)
        {
            var hit = Deepest(stage, x, y);
            if (hit == null) return null;
            // climb to an interactive object (Sprite / MovieClip / TextField) with mouseEnabled
            DisplayObject target = hit;
            while (target != null && !(target is DisplayObjectContainer || target is TextField)) target = target.parent;
            while (target != null && !(target is Stage) && !target.mouseEnabled) target = target.parent;
            // an ancestor with mouseChildren = false captures events for its subtree
            for (var p = target?.parent; p != null; p = p.parent)
                if (!p.mouseChildren && p.mouseEnabled) target = p;
            return target;
        }

        /// <summary>Debug description of what the pointer hits at (x, y).</summary>
        public static string DescribeAt(Stage stage, double x, double y)
        {
            var hit = Deepest(stage, x, y);
            var sb = new System.Text.StringBuilder("deepest=");
            for (var o = hit; o != null; o = o.parent)
                sb.Append(o.GetType().Name).Append('(').Append(o.name).Append(o.mouseEnabled ? "" : " !me")
                  .Append(o is DisplayObjectContainer c && !c.mouseChildren ? " !mc" : "").Append(o.visible ? "" : " !vis").Append(") < ");
            sb.Append(" target=").Append(InteractiveAt(stage, x, y));
            return sb.ToString();
        }

        static DisplayObject Deepest(DisplayObject o, double x, double y)
        {
            if (!o.visible || o.IsMaskContent) return null;
            if (o is DisplayObjectContainer c)
            {
                if (c is Sprite sp && sp.hitArea != null && sp.hitArea.getBounds(sp.stage).contains(x, y)) return sp;
                for (int i = c.Children.Count - 1; i >= 0; i--)
                {
                    var h = Deepest(c.Children[i], x, y);
                    if (h != null) return h;
                }
                if (c is Sprite s2 && s2.GraphicsOrNull != null && s2.GraphicsOrNull.Polys.Count > 0 && c.getBounds(c.stage).contains(x, y)) return c;
                return null;
            }
            if (o is ImageInstance ii && ii.Image.Page < 0) return null; // invisible area shapes do not catch clicks
            var b = o.getBounds(o.stage);
            return b.contains(x, y) ? o : null;
        }
    }
}
