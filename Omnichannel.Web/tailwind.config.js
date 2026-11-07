/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Views/**/*.cshtml",
    "./Areas/**/*.cshtml",
    "./wwwroot/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        'primary-rose': '#E8618C',
        'deep-berry': '#B8336A',
        'petal-cream': '#FFF4F7',
        'ink': '#2B1B24',
        'pastel-rose': '#FFF4F7',
        'pastel-nude': '#faecef',
        'rose-gold': '#E8618C',
        'nude-gold': '#C5A880',
        'champagne-gold': '#d4af37',
        'charcoal': '#2B1B24',
        'dark-muted': '#5C3A4D',
        'soft-border': '#F7C6D4',
        'blush': '#F7C6D4',
        'magenta-accent': '#B8336A',
      },
      fontFamily: {
        'brand': ['Quicksand', 'Plus Jakarta Sans', 'sans-serif'],
        'serif': ['Playfair Display', 'Cormorant Garamond', 'Georgia', 'serif'],
        'sans': ['Plus Jakarta Sans', 'Inter', 'system-ui', 'sans-serif'],
      },
      animation: {
        'drawer-slide-in': 'slideIn 0.3s cubic-bezier(0.16, 1, 0.3, 1)',
        'fade-in': 'fadeIn 0.3s ease',
        'shimmer-badge': 'shimmer 2s infinite linear',
      },
      keyframes: {
        slideIn: {
          '0%': { transform: 'translateX(100%)' },
          '100%': { transform: 'translateX(0)' },
        },
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        shimmer: {
          '0%': { transform: 'translateX(-100%)' },
          '100%': { transform: 'translateX(100%)' },
        }
      }
    },
  },
  plugins: [],
}
