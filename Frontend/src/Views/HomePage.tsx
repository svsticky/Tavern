import { defineComponent } from "vue";

export default defineComponent({
  setup() {
    return () => (
      <div class="p-6 max-w-2xl mx-auto">
        <h1 class="text-3xl font-bold mb-3">Tavern — Home (Debug)</h1>
        <p class="mb-4">Simple homepage to verify the frontend is working.</p>
      </div>
    );
  },
});
