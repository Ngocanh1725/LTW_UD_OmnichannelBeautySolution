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
        'pastel-rose': '#fdf2f4',
        'pastel-nude': '#faecef',
        'rose-gold': '#b76e79',
        'nude-gold': '#C5A880',
        'champagne-gold': '#d4af37',
        'charcoal': '#18181b',
        'dark-muted': '#3f3f46',
        'soft-border': '#f1d8dd',
        'blush': '#f5d6da',
        'magenta-accent': '#d63384',
      },
      fontFamily: {
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
